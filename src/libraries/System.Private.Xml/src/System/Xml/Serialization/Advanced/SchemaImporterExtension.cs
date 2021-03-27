// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization.Advanced
{

    using System.Xml.Schema;
    using System.Xml;
    using System.Collections;
    using System.Collections.Specialized;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class SchemaImporterExtension
    {

        public virtual string? ImportSchemaType(string name, string ns, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }


        public virtual string? ImportSchemaType(XmlSchemaType type, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }


        public virtual string? ImportAnyElement(XmlSchemaAny any, bool mixed, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }


        public virtual CodeExpression? ImportDefaultValue(string? value, string? type)
        {
            return null;
        }
    }

    public class SchemaImporterExtensionCollection : CollectionBase
    {
        private Hashtable? exNames;

        internal Hashtable Names
        {
            get
            {
                if (exNames == null)
                    exNames = new Hashtable();
                return exNames;
            }
        }

        public int Add(SchemaImporterExtension extension)
        {
            return Add(extension.GetType().FullName, extension);
        }

        public int Add(string? name, Type type)
        {
            if (type.IsSubclassOf(typeof(SchemaImporterExtension)))
            {
                return Add(name, (SchemaImporterExtension?)Activator.CreateInstance(type));
            }
            else
            {
                throw new ArgumentException(SR.GetResourceString(SR.XmlInvalidSchemaExtension, type.ToString()));
            }
        }

        public void Remove(string name)
        {
            if (Names[name] != null)
            {
                List.Remove(Names[name]);
                Names[name] = null;
            }
        }

        public new void Clear()
        {
            Names.Clear();
            List.Clear();
        }

        internal SchemaImporterExtensionCollection Clone()
        {
            SchemaImporterExtensionCollection clone = new SchemaImporterExtensionCollection();
            clone.exNames = (Hashtable)this.Names.Clone();
            foreach (object o in this.List)
            {
                clone.List.Add(o);
            }
            return clone;
        }

        public SchemaImporterExtension? this[int index]
        {
            get { return (SchemaImporterExtension?)List[index]; }
            set { List[index] = value; }
        }

        internal int Add(string? name, SchemaImporterExtension? extension)
        {
            if (name is not null && Names[name] != null)
            {
                if (Names[name]?.GetType() != extension?.GetType())
                {
                    throw new InvalidOperationException(SR.GetResourceString(SR.XmlConfigurationDuplicateExtension, name));
                }
                return -1;
            }
            if (name is not null)
            {
                Names[name] = extension;
                return List.Add(extension);
            }
            return -1;
        }

        public void Insert(int index, SchemaImporterExtension extension)
        {
            List.Insert(index, extension);
        }

        public int IndexOf(SchemaImporterExtension extension)
        {
            return List.IndexOf(extension);
        }

        public bool Contains(SchemaImporterExtension extension)
        {
            return List.Contains(extension);
        }

        public void Remove(SchemaImporterExtension extension)
        {
            List.Remove(extension);
        }

        public void CopyTo(SchemaImporterExtension[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }

    internal class MappedTypeDesc
    {
        private string name;
        private string ns;
        private XmlSchemaType xsdType;
        private XmlSchemaObject context;
        private string clrType;
        private SchemaImporterExtension extension;
        private CodeNamespace code;
        private bool exported;
        private StringCollection references;

        internal MappedTypeDesc(string clrType, string name, string ns, XmlSchemaType xsdType, XmlSchemaObject context, SchemaImporterExtension extension, CodeNamespace code, StringCollection references)
        {
            this.clrType = clrType.Replace('+', '.');
            this.name = name;
            this.ns = ns;
            this.xsdType = xsdType;
            this.context = context;
            this.code = code;
            this.references = references;
            this.extension = extension;
        }

        internal SchemaImporterExtension Extension { get { return extension; } }
        internal string Name { get { return clrType; } }

        internal StringCollection ReferencedAssemblies
        {
            get
            {
                if (references == null)
                    references = new StringCollection();
                return references;
            }
        }

        internal CodeTypeDeclaration? ExportTypeDefinition(CodeNamespace codeNamespace, CodeCompileUnit? codeCompileUnit)
        {
            if (exported)
                return null;
            exported = true;

            foreach (CodeNamespaceImport import in code.Imports)
            {
                codeNamespace.Imports.Add(import);
            }
            CodeTypeDeclaration? codeClass = null;
            string comment = SR.GetResourceString(SR.XmlExtensionComment, extension.GetType().FullName);
            foreach (CodeTypeDeclaration type in code.Types)
            {
                if (clrType == type.Name)
                {
                    if (codeClass != null)
                        throw new InvalidOperationException(SR.GetResourceString(SR.XmlExtensionDuplicateDefinition, $"{extension.GetType().FullName} {clrType}"));
                    codeClass = type;
                }
                type.Comments.Add(new CodeCommentStatement(comment, false));
                codeNamespace.Types.Add(type);
            }
            if (codeCompileUnit != null)
            {
                foreach (string? reference in ReferencedAssemblies)
                {
                    if (codeCompileUnit.ReferencedAssemblies.Contains(reference))
                        continue;
                    codeCompileUnit.ReferencedAssemblies.Add(reference);
                }
            }
            return codeClass;
        }
    }
}
