﻿using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Dynamic;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicTypeLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicTypeEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.TypeName,
                    });

                DynamicTypeGraph.Register();
                DynamicLogic.GetCodeFiles += GetCodeFiles;
                DynamicLogic.OnWriteDynamicStarter += WriteDynamicStarter;
            }
        }

        public class DynamicTypeGraph : Graph<DynamicTypeEntity>
        {
            public static void Register()
            {
                new Construct(DynamicTypeOperation.Create)
                {
                    Construct = (_) => new DynamicTypeEntity { },
                }.Register();

                new ConstructFrom<DynamicTypeEntity>(DynamicTypeOperation.Clone)
                {
                    Construct = (e, _) => {

                        var def = e.GetDefinition();
                        var result = new DynamicTypeEntity { TypeName = null };
                        result.SetDefinition(def);
                        return result;
                    },
                }.Register();

                new Execute(DynamicTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => {

                        if (!e.IsNew)
                        {
                            var old = e.ToLite().Retrieve();
                            if (e.TypeName != old.TypeName)
                                DynamicSqlMigrationLogic.AddDynamicRename(Replacements.KeyTables, old.TypeName, e.TypeName);

                            var newDef = e.GetDefinition();
                            var oldDef = old.GetDefinition();

                            var pairs = newDef.Properties
                                .Join(oldDef.Properties, n => n.UID, o => o.UID, (n, o) => new { n, o })
                                .Where(a => a.n.Type == a.o.Type);
                            
                            foreach (var a in pairs.Where(a =>  a.n.Name != a.o.Name))
                            {
                                DynamicSqlMigrationLogic.AddDynamicRename(Replacements.KeyColumnsForTable(old.TypeName),
                                    a.o.Name, a.n.Name);
                            }
                        }
                    },
                }.Register();

                new Delete(DynamicTypeOperation.Delete)
                {
                    Delete = (e, _) =>
                    {
                        e.Delete();
                    }
                }.Register();
            }
        }

        public static string GetPropertyType(DynamicProperty property)
        {
            var generator = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenEntitiesNamespace, null, null, new HashSet<string>());

            return generator.GetPropertyType(property);
        }

        internal static List<DynamicTypeEntity> GetTypes()
        {
            CacheLogic.GloballyDisabled = true;
            try
            {
                return ExecutionMode.Global().Using(a => Database.Query<DynamicTypeEntity>().ToList());
            }
            finally
            {
                CacheLogic.GloballyDisabled = false;
            }
        }

        public static void WriteDynamicStarter(StringBuilder sb, int indent) {

            var types = GetTypes();
            foreach (var item in types)
                sb.AppendLine($"{item}Logic.Start(sb, dqm);".Indent(indent));
        }

        public static Func<Dictionary<string, Dictionary<string, string>>> GetAlreadyTranslatedExpressions;

        public static List<CodeFile> GetCodeFiles()
        {
            if (!Administrator.ExistTable<DynamicTypeEntity>())
                return new List<CodeFile>();

            List<DynamicTypeEntity> types = GetTypes();
            var alreadyTranslatedExpressions = GetAlreadyTranslatedExpressions?.Invoke();

            var entities =  types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dcg = new DynamicTypeCodeGenerator(DynamicLogic.CodeGenEntitiesNamespace, dt.TypeName, def, DynamicLogic.Namespaces);

                var content = dcg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + ".cs",
                    FileContent = content
                };
            }).ToList();
             
            var logics = types.Select(dt =>
            {
                var def = dt.GetDefinition();

                var dlg = new DynamicTypeLogicGenerator(DynamicLogic.CodeGenEntitiesNamespace, dt.TypeName, def, DynamicLogic.Namespaces)
                {
                    AlreadyTranslated = alreadyTranslatedExpressions?.TryGetC(dt.TypeName + "Entity"),
                };

                var content = dlg.GetFileCode();
                return new CodeFile
                {
                    FileName = dt.TypeName + "Logic.cs",
                    FileContent = content
                };
            }).ToList();

            return entities.Concat(logics).ToList();
        }
    }

    public class DynamicTypeCodeGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public DynamicTypeDefinition Def { get; private set; }

        public DynamicTypeCodeGenerator(string @namespace, string typeName, DynamicTypeDefinition def, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeName = typeName;
            this.Def = def;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));

            sb.AppendLine();
            sb.AppendLine("namespace " + this.Namespace);
            sb.AppendLine("{");
            sb.Append(GetEntityCode().Indent(4));
         
            var ops = GetEntityOperation();
            if (ops != null)
            {
                sb.AppendLine();
                sb.Append(ops.Indent(4));
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GetEntityCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var gr in GetEntityAttributes().GroupsOf(a => a.Length, 100))
            {
                sb.AppendLine("[" + gr.ToString(", ") + "]");
            }
            sb.AppendLine($"public class {this.TypeName}Entity : {GetEntityBaseClass(Def.BaseType)}");
            sb.AppendLine("{");

            foreach (var prop in Def.Properties)
            {
                string field = WriteProperty(prop);

                if (field != null)
                {
                    sb.Append(field.Indent(4));
                    sb.AppendLine();
                }
            }

            string toString = GetToString();
            if (toString != null)
            {
                sb.Append(toString.Indent(4));
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString();
        }

        public string GetEntityOperation()
        {
            if (this.Def.OperationCreate == null &&
                 this.Def.OperationSave == null &&
                 this.Def.OperationDelete == null)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AutoInit]"); //Only for ReflectionServer
            sb.AppendLine($"public static class {this.TypeName}Operation");
            sb.AppendLine("{");

            if (this.Def.OperationCreate != null)
                sb.AppendLine($"    public static readonly ConstructSymbol<{this.TypeName}Entity>.Simple Create = OperationSymbol.Construct<{this.TypeName}Entity>.Simple(typeof({ this.TypeName}Operation), \"Create\");");


            var requiresSaveOperation = (this.Def.EntityKind != null && EntityKindAttribute.CalculateRequiresSaveOperation(this.Def.EntityKind.Value));
            if ((this.Def.OperationSave != null) && !requiresSaveOperation)
                throw new InvalidOperationException($"DynamicType '{this.TypeName}' defines Save but has EntityKind = '{this.Def.EntityKind}'");
            else if (this.Def.OperationSave == null && requiresSaveOperation)
                throw new InvalidOperationException($"DynamicType '{this.TypeName}' does not define Save but has EntityKind = '{this.Def.EntityKind}'");

            if (this.Def.OperationSave != null)
                sb.AppendLine($"    public static readonly ExecuteSymbol<{this.TypeName}Entity> Save = OperationSymbol.Execute<{this.TypeName}Entity>(typeof({ this.TypeName}Operation), \"Save\");");

            if (this.Def.OperationDelete != null)
                sb.AppendLine($"    public static readonly DeleteSymbol<{this.TypeName}Entity> Delete = OperationSymbol.Delete<{this.TypeName}Entity>(typeof({ this.TypeName}Operation), \"Delete\");");

            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GetEntityBaseClass(DynamicBaseType baseType)
        {
            switch (baseType)
            {
                case DynamicBaseType.Entity: return "Entity";
                default: throw new NotImplementedException();
            }
        }

        protected virtual string GetToString()
        {
            if (Def.ToStringExpression == null)
                return null;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"static Expression<Func<{TypeName}Entity, string>> ToStringExpression = e => {Def.ToStringExpression};");
            sb.AppendLine("[ExpressionField(\"ToStringExpression\")]");
            sb.AppendLine("public override string ToString()");
            sb.AppendLine("{");
            sb.AppendLine("    return ToStringExpression.Evaluate(this);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private List<string> GetEntityAttributes()
        {
            List<string> atts = new List<string> { "Serializable" };

            atts.Add("EntityKind(EntityKind." + Def.EntityKind.Value + ", EntityData." + Def.EntityData.Value + ")");

            if (Def.TableName.HasText())
            {
                var objName = ObjectName.Parse(Def.TableName);

                var parts = new List<string>
                {
                     Literal(objName.Name),
                     objName.Schema != null ? "SchemaName =" + Literal(objName.Schema.Name) : null,
                     objName.Schema.Database != null ? "DatabaseName =" + Literal(objName.Schema.Database.Name) : null,
                     objName.Schema.Database.Server != null ? "ServerName =" + Literal(objName.Schema.Database.Server.Name) : null,
                }.NotNull().ToString(", ");

                atts.Add("TableName(" + parts + ")");
            }

            if (Def.PrimaryKey != null)
            {
                var name = Def.PrimaryKey.Name;
                var type = Def.PrimaryKey.Type;
                var identity = Def.PrimaryKey.Identity;

                atts.Add($"PrimaryKey(typeof({ (type.HasText() ? type.GetType().TypeName() : "int") }), { (name.HasText() ? '"' + name + '"' : "\"ID\"") }, Identity = {identity.ToString().ToLower()})");
            }

            if (Def.Ticks != null)
            {
                var hasTicks = Def.Ticks.HasTicks;
                var name = Def.Ticks.Name;
                var type = Def.Ticks.Type;

                if (!hasTicks)
                    atts.Add("TicksColumn(false)");
                else
                    atts.Add($"TicksColumn({hasTicks}, Name = { (name.HasText() ? name : "int") }, Type = typeof({ (type.HasText() ? type : "int") }))");
            }

            return atts;
        }

        string Literal(object obj)
        {
            return CSharpRenderer.Value(obj, obj.GetType(), null);
        }

        protected virtual string WriteProperty(DynamicProperty property)
        {
            string type = GetPropertyType(property);

            StringBuilder sb = new StringBuilder();

            string inititalizer = (property.IsMList != null) ? $" = new {type}()": null;
            string fieldName = property.Name.FirstLower();

            WriteAttributeTag(sb, GetFieldAttributes(property));
            sb.AppendLine($"{type} {fieldName}{inititalizer};");
            WriteAttributeTag(sb, GetPropertyAttributes(property));
            sb.AppendLine($"public {type} {property.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    get {{ return this.Get({fieldName}); }}");
            sb.AppendLine($"    set {{ this.Set(ref {fieldName}, value); }}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private IEnumerable<string> GetPropertyAttributes(DynamicProperty property)
        {
            return property.Validators.EmptyIfNull().Select(v => GetValidatorAttribute(v));
        }

        private string GetValidatorAttribute(DynamicValidator v)
        {
            var name = v.Type + "Validator";

            var extra = v.ExtraArguments();

            if (extra == null)
                return name;

            return $"{name}({extra})";
        }

        protected virtual void WriteAttributeTag(StringBuilder sb, IEnumerable<string> attributes)
        {
            foreach (var gr in attributes.GroupsOf(a => a.Length, 100))
            {
                sb.AppendLine("[" + gr.ToString(", ") + "]");
            }
        }

        private List<string> GetFieldAttributes(DynamicProperty property)
        {
            List<string> atts = new List<string>();
            if (property.IsNullable != IsNullable.Yes)
                atts.Add("NotNullable");

            if (property.Size != null || property.Scale != null)
            {
                SqlDbType dbType;
                var props = new[]
                {
                    property.Size != null ? "Size = " + Literal(property.Size) : null,
                    property.Scale != null ? "Scale = " + Literal(property.Scale) : null,
                    property.ColumnType.HasText() ?  "SqlDbType = " +Literal(Enum.TryParse<SqlDbType>(property.ColumnType, out dbType)? dbType : SqlDbType.Udt) : null,
                    property.ColumnType.HasText() && !Enum.TryParse<SqlDbType>(property.ColumnType, out dbType) ?  "UserDefinedTypeName = " + Literal(property.ColumnType) : null,
                     
                }.NotNull().ToString(", ");

                atts.Add($"SqlDbType({props})");
            }

            if (property.ColumnName.HasText())
                atts.Add("ColumnName(" + Literal(property.ColumnName) + ")");

            switch (property.UniqueIndex)
            {
                case Entities.Dynamic.UniqueIndex.No: break;
                case Entities.Dynamic.UniqueIndex.Yes: atts.Add("UniqueIndex"); break;
                case Entities.Dynamic.UniqueIndex.YesAllowNull: atts.Add("UniqueIndex(AllowMultipleNulls = true)"); break;
            }

            return atts;
        }

        public virtual string GetPropertyType(DynamicProperty property)
        {
            if (string.IsNullOrEmpty(property.Type))
                return "";

            string result = SimplifyType(property.Type);

            var t = TryResolveType(property.Type);
            
            if (property.IsNullable != IsNullable.No && t?.IsValueType == true)
                result = result + "?";

            if (property.IsLite)
                result = "Lite<" + result + ">";
            
            if (property.IsMList != null)
                result = "MList<" + result + ">";

            return result;
        }

        private string SimplifyType(string type)
        {
            var ns = type.TryBeforeLast(".");

            if (ns == null)
                return type;

            if (this.Namespace == ns || this.Usings.Contains(ns))
                return type.AfterLast(".");

            return type;
        }

        public Type TryResolveType(string typeName)
        {
            switch (typeName)
            {
                case "bool": return typeof(bool);
                case "byte": return typeof(byte);
                case "char": return typeof(char);
                case "decimal": return typeof(decimal);
                case "double": return typeof(double);
                case "short": return typeof(short);
                case "int": return typeof(int);
                case "long": return typeof(long);
                case "sbyte": return typeof(sbyte);
                case "float": return typeof(float);
                case "string": return typeof(string);
                case "ushort": return typeof(ushort);
                case "uint": return typeof(uint);
                case "ulong": return typeof(ulong);
            }


            var result = Type.GetType("System." + typeName);

            if (result != null)
                return result;

            var type = TypeLogic.TryGetType(typeName);
            if (type != null)
                return type;

            return null;
        }
    }

    public class DynamicTypeLogicGenerator
    {
        public HashSet<string> Usings { get; private set; }
        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public DynamicTypeDefinition Def { get; private set; }

        public Dictionary<string, string> AlreadyTranslated { get; set; }

        public DynamicTypeLogicGenerator(string @namespace, string typeName, DynamicTypeDefinition def, HashSet<string> usings)
        {
            this.Usings = usings;
            this.Namespace = @namespace;
            this.TypeName = typeName;
            this.Def = def;
        }

        public string GetFileCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.Usings)
                sb.AppendLine("using {0};".FormatWith(item));


            sb.AppendLine();
            sb.AppendLine($"namespace {this.Namespace}");
            sb.AppendLine($"{{");


            var complexFields = this.Def.QueryFields.EmptyIfNull().Select(a => GetComplexQueryField(a)).NotNull().ToList();
            var complexNotTranslated = complexFields.Where(a => this.AlreadyTranslated?.TryGetC(a) == null).ToList();
            if (complexNotTranslated.Any())
            {
                sb.AppendLine($"    public enum CodeGenQuery{this.TypeName}Message");
                sb.AppendLine($"    {{");
                foreach (var item in complexNotTranslated)
                    sb.AppendLine($"        " + item + ",");
                sb.AppendLine($"    }}");
            }

            sb.AppendLine($"    public static class {this.TypeName}Logic");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))");
            sb.AppendLine($"            {{");
            sb.AppendLine(GetInclude().Indent(16));

            if (complexFields != null)
                sb.AppendLine(RegisterComplexQuery(complexFields).Indent(16));

            var complexOperations = RegisterComplexOperations();
            if (complexOperations != null)
                sb.AppendLine(complexOperations.Indent(16));

            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }

        private string GetInclude()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"sb.Include<{this.TypeName}Entity>()");

            if (this.Def.OperationSave != null && string.IsNullOrWhiteSpace(this.Def.OperationSave.Execute.Trim()))
                sb.AppendLine($"    .WithSave({this.TypeName}Operation.Save)");

            if (this.Def.OperationDelete != null && string.IsNullOrWhiteSpace(this.Def.OperationDelete.Delete.Trim()))
                sb.AppendLine($"    .WithDelete({this.TypeName}Operation.Delete)");

            var mcui = this.Def.MultiColumnUniqueIndex;
            if (mcui != null)
                sb.AppendLine($"    .WithUniqueIndex(e => new {{{ mcui.Fields.ToString(", ")}}}{(mcui.Where.HasText() ? ", " + mcui.Where : "")})");

            var queryFields = this.Def.QueryFields.EmptyIfNull();

            if (queryFields.EmptyIfNull().Any() && queryFields.All(a => GetComplexQueryField(a) == null))
            {
                var lines = new[] { "Entity = e" }.Concat(queryFields);

                sb.AppendLine($@"    .WithQuery(dqm, e => new 
    {{ 
{ lines.ToString(",\r\n").Indent(8)}
    }})");
            }

            sb.Insert(sb.Length - 2, ';');
            return sb.ToString();
        }

        public static string GetComplexQueryField(string field)
        {
            var fieldName = field.TryBefore("=")?.Trim();

            if (fieldName == null)
                return null;

            if (!Reflector.ValidIdentifier(fieldName))
                return null;

            var lastProperty = field.After("=").TryAfterLast(".")?.Trim();

            if (lastProperty == null || fieldName != lastProperty)
                return fieldName;

            return null;
        }

        public string RegisterComplexQuery(List<string> complexQueryFields)
        {
            StringBuilder sb = new StringBuilder();

            var lines = new[] { "Entity = e" }.Concat(this.Def.QueryFields);

            sb.AppendLine($@"dqm.RegisterQuery(typeof({this.TypeName}Entity), () => DynamicQueryCore.Auto(
    from e in Database.Query<{this.TypeName}Entity>()
    select new
    {{
{ lines.ToString(",\r\n").Indent(8)}
    }})
{complexQueryFields.Select(f => $".ColumnDisplayName(a => a.{f}, {this.AlreadyTranslated?.TryGetC(f) ?? $"CodeGenQuery{this.TypeName}Message.{f}"})").ToString("\r\n").Indent(4)}
    );");

            sb.AppendLine();
            return sb.ToString();
        }

        private string RegisterComplexOperations()
        {
            StringBuilder sb = new StringBuilder();
            var operationConstruct = this.Def.OperationCreate?.Construct.Trim();
            if (!string.IsNullOrWhiteSpace(operationConstruct))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Construct({0}Operation.Create)".FormatWith(this.TypeName));
                sb.AppendLine("{");
                sb.AppendLine("    Construct = (args) => {\r\n" + operationConstruct.Indent(8) + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            var operationExecute = this.Def.OperationSave?.Execute.Trim();
            var operationCanExecute = this.Def.OperationSave?.CanExecute?.Trim();
            if (!string.IsNullOrWhiteSpace(operationExecute) || !string.IsNullOrWhiteSpace(operationCanExecute))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Execute({0}Operation.Save)".FormatWith(this.TypeName));
                sb.AppendLine("{");

                if (!string.IsNullOrWhiteSpace(operationCanExecute))
                    sb.AppendLine($"    CanExecute = e => {operationCanExecute},");

                sb.AppendLine("    AllowsNew = true,");
                sb.AppendLine("    Lite = false,");
                sb.AppendLine("    Execute = (e, args) => {\r\n" + operationExecute?.Indent(8) + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            var operationDelete = this.Def.OperationDelete?.Delete.Trim();
            var operationCanDelete = this.Def.OperationDelete?.CanDelete?.Trim();
            if (!string.IsNullOrWhiteSpace(operationDelete) || !string.IsNullOrEmpty(operationCanDelete))
            {
                sb.AppendLine();
                sb.AppendLine("new Graph<{0}Entity>.Delete({0}Operation.Delete)".FormatWith(this.TypeName));
                sb.AppendLine("{");

                if (!string.IsNullOrWhiteSpace(operationCanDelete))
                    sb.AppendLine($"    CanDelete = e => {operationCanDelete},");

                sb.AppendLine("    Delete = (e, args) => {\r\n" + (operationDelete.DefaultText("e.Delete();")).Indent(8) + "\r\n}");
                sb.AppendLine("}.Register();");
            }

            return sb.ToString();
        }
    }

}
