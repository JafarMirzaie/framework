using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Engine.Authorization
{
    public interface IAutoCreateUserContext
    {
        public string UserName { get; }
        public string? EmailAddress { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public Guid? OID { get;  }
    }

    public class DirectoryServiceAutoCreateUserContext : IAutoCreateUserContext
    {
        public readonly PrincipalContext PrincipalContext;
        public string UserName { get; private set; }
        public string DomainName { get; private set; }
        public string? EmailAddress => this.GetUserPrincipal().EmailAddress;

        public string FirstName => this.GetUserPrincipal().GivenName;

        public string LastName => this.GetUserPrincipal().Surname;

        public Guid? OID => null;

        UserPrincipal? userPrincipal;

        public DirectoryServiceAutoCreateUserContext(PrincipalContext principalContext, string localName, string domainName)
        {
            PrincipalContext = principalContext;
            UserName = localName;
            DomainName = domainName;
        }

        public UserPrincipal GetUserPrincipal() //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account
        {
            return userPrincipal ?? (userPrincipal = UserPrincipal.FindByIdentity(PrincipalContext, DomainName + @"\" + UserName));
        }
    }

    public class AzureClaimsAutoCreateUserContext : IAutoCreateUserContext
    {
        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        string GetClaim(string type) => ClaimsPrincipal.Claims.SingleEx(a => a.Type == type).Value;

        public Guid? OID => Guid.Parse(GetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier"));

        public string UserName => GetClaim("preferred_username");
        public string? EmailAddress => GetClaim("preferred_username");

        public string FirstName
        {
            get
            {
                var name = ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == "name")?.Value;

                return name == null ? "Unknown" : name.Contains(",") ? name.After(",").Trim() : name.TryBefore(" ")?.Trim() ?? name.DefaultToNull() ?? "Unknown";
            }
        }

        public string LastName
        {
            get
            {
                var name = ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == "name")?.Value;

                return name == null ? "Unknown" : name.Contains(",") ? name.Before(",").Trim() : name.TryAfter(" ")?.Trim() ??  "Unknown";
            }
        }

        public AzureClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal)
        {
            this.ClaimsPrincipal = claimsPrincipal;
        }
    }

    public class ActiveDirectoryAuthorizer : ICustomAuthorizer
    {
        public Func<ActiveDirectoryConfigurationEmbedded> GetConfig;

        public ActiveDirectoryAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig)
        {
            this.GetConfig = getConfig;
        }

        public virtual UserEntity Login(string userName, string password, out string authenticationType)
        {
            var passwordHash = Security.EncodePassword(password);
            if (AuthLogic.TryRetrieveUser(userName, passwordHash) != null)
                return AuthLogic.Login(userName, passwordHash, out authenticationType); //Database is faster than Active Directory

            UserEntity? user = LoginWithActiveDirectoryRegistry(userName, password);
            if (user != null)
            {
                authenticationType = "adRegistry";
                return user;
            }

            return AuthLogic.Login(userName, Security.EncodePassword(password), out authenticationType);
        }

        public virtual UserEntity? LoginWithActiveDirectoryRegistry(string userName, string password)
        {
            using (AuthLogic.Disable())
            {
                var config = this.GetConfig();
                var domainName = userName.TryAfterLast('@') ?? userName.TryBefore('\\') ?? config.DomainName;
                var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

                if (domainName != null && config.LoginWithActiveDirectoryRegistry)
                {
                    try
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName, localName, password))
                        {
                            if (pc.ValidateCredentials(localName + "@" + domainName, password, ContextOptions.Negotiate))
                            {
                                UserEntity? user = AuthLogic.RetrieveUser(userName);

                                if (user == null)
                                {
                                    user = OnAutoCreateUser(new DirectoryServiceAutoCreateUserContext(pc, localName, domainName!));
                                }


                                if (user != null)
                                {
                                    AuthLogic.OnUserLogingIn(user);
                                    return user;
                                }
                                else
                                {
                                    throw new InvalidOperationException(ActiveDirectoryAuthorizerMessage.ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication.NiceToString(localName));
                                }
                            }
                        }
                    }
                    catch (PrincipalServerDownException)
                    {
                        // Do nothing for this kind of Active Directory exception
                    }
                }

                return null;
            }
        }

        public virtual UserEntity? OnAutoCreateUser(IAutoCreateUserContext ctx)
        {
            if (!GetConfig().AutoCreateUsers)
                return null;

            var user = this.AutoCreateUserInternal(ctx);
            if (user != null && user.IsNew)
            {
                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    user.Save();
                }
            }

            return user;
        }

        public virtual UserEntity? AutoCreateUserInternal(IAutoCreateUserContext ctx)
        {
            var result = new UserEntity
            {
                UserName = ctx.UserName,
                PasswordHash = null,
                Email = ctx.EmailAddress,
                Role = GetRole(ctx, throwIfNull: true)!,
                State = UserState.Saved,
            };

            var mixin = result.TryMixin<UserOIDMixin>();
            if (mixin != null)
                mixin.OID = ctx.OID;

            return result;
        }

        public virtual Lite<RoleEntity>? GetRole(IAutoCreateUserContext ctx, bool throwIfNull)
        {
            var config = GetConfig();
            if (ctx is DirectoryServiceAutoCreateUserContext ds)
            {
                var groups = ds.GetUserPrincipal().GetGroups();
                var role = config.RoleMapping.FirstOrDefault(m =>
                {
                    Guid.TryParse(m.ADNameOrGuid, out var guid);
                    return groups.Any(g => g.Name == m.ADNameOrGuid || g.Guid == guid);
                })?.Role ?? config.DefaultRole;

                if (role != null)
                    return role;

                if (throwIfNull)
                    throw new InvalidOperationException("No Default Role set and no matching RoleMapping found for any role: \r\n" + groups.ToString(a => a.Name, "\r\n"));
                else
                    return null;
            }
            else
            {
                if (config.DefaultRole != null)
                    return config.DefaultRole;

                if (throwIfNull)
                    throw new InvalidOperationException("No default role set");
                else
                    return null;

            }
        }
    }
}
