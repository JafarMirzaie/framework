﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Entities.UserAssets
{
    [Serializable]
    public sealed class QueryTokenEntity : EmbeddedEntity, IEquatable<QueryTokenEntity>
    {
        private QueryTokenEntity()
        {
        }

        public QueryTokenEntity(QueryToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            this.token = token;
        }

        public QueryTokenEntity(string tokenString)
        {
            if (string.IsNullOrEmpty(tokenString))
                throw new ArgumentNullException("tokenString");

            this.tokenString = tokenString;
        }

        [NotNullable]
        string tokenString;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string TokenString
        {
            get { return tokenString; }
        }

        [Ignore]
        QueryToken token;
        [HiddenProperty]
        public QueryToken Token
        {
            get
            {
                if (parseException != null && token == null)
                    throw parseException;

                return token;
            }
        }

        [HiddenProperty]
        public QueryToken TryToken
        {
            get { return token; }
        }

        [Ignore]
        Exception parseException;
        [HiddenProperty]
        public Exception ParseException
        {
            get { return parseException; }
        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token == null ? null : token.FullKey();
        }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            try
            {
                token = QueryUtils.Parse(tokenString, description, options);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".FormatWith(context.GetType().Name, context.IdOrNull, context, e.Message), e);
            }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => TokenString) && token == null)
            {
                return parseException != null ? parseException.Message : ValidationMessage._0IsNotSet.NiceToString().FormatWith(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            if (token != null)
                return token.FullKey();

            return tokenString;
        }

        public bool Equals(QueryTokenEntity other)
        {
            return this.GetTokenString() == other.GetTokenString();
        }

        public string GetTokenString()
        {
            return this.token != null ? this.token.FullKey() : this.tokenString;
        }
        
        public override bool Equals(object obj)
        {
            return obj is QueryTokenEntity && this.Equals((QueryTokenEntity)obj);
        }

        public override int GetHashCode()
        {
            return this.Token.GetHashCode();
        }
    }

}
