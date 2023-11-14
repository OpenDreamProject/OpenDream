﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
namespace OpenDreamRuntime.Procs.DebugAdapter.Coverage {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class coverage {
        
        private string[] sourcesField;
        
        private package[] packagesField;
        
        private string linerateField;
        
        private string branchrateField;
        
        private string linescoveredField;
        
        private string linesvalidField;
        
        private string branchescoveredField;
        
        private string branchesvalidField;
        
        private string complexityField;
        
        private string versionField;
        
        private string timestampField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("source", IsNullable=false)]
        public string[] sources {
            get {
                return this.sourcesField;
            }
            set {
                this.sourcesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("package", IsNullable=false)]
        public package[] packages {
            get {
                return this.packagesField;
            }
            set {
                this.packagesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("line-rate")]
        public string linerate {
            get {
                return this.linerateField;
            }
            set {
                this.linerateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branch-rate")]
        public string branchrate {
            get {
                return this.branchrateField;
            }
            set {
                this.branchrateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("lines-covered")]
        public string linescovered {
            get {
                return this.linescoveredField;
            }
            set {
                this.linescoveredField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("lines-valid")]
        public string linesvalid {
            get {
                return this.linesvalidField;
            }
            set {
                this.linesvalidField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branches-covered")]
        public string branchescovered {
            get {
                return this.branchescoveredField;
            }
            set {
                this.branchescoveredField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branches-valid")]
        public string branchesvalid {
            get {
                return this.branchesvalidField;
            }
            set {
                this.branchesvalidField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string complexity {
            get {
                return this.complexityField;
            }
            set {
                this.complexityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string timestamp {
            get {
                return this.timestampField;
            }
            set {
                this.timestampField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class package {
        
        private @class[] classesField;
        
        private string nameField;
        
        private string linerateField;
        
        private string branchrateField;
        
        private string complexityField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("class", IsNullable=false)]
        public @class[] classes {
            get {
                return this.classesField;
            }
            set {
                this.classesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("line-rate")]
        public string linerate {
            get {
                return this.linerateField;
            }
            set {
                this.linerateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branch-rate")]
        public string branchrate {
            get {
                return this.branchrateField;
            }
            set {
                this.branchrateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string complexity {
            get {
                return this.complexityField;
            }
            set {
                this.complexityField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class @class {
        
        private method[] methodsField;
        
        private line[] linesField;
        
        private string nameField;
        
        private string filenameField;
        
        private string linerateField;
        
        private string branchrateField;
        
        private string complexityField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("method", IsNullable=false)]
        public method[] methods {
            get {
                return this.methodsField;
            }
            set {
                this.methodsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("line", IsNullable=false)]
        public line[] lines {
            get {
                return this.linesField;
            }
            set {
                this.linesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string filename {
            get {
                return this.filenameField;
            }
            set {
                this.filenameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("line-rate")]
        public string linerate {
            get {
                return this.linerateField;
            }
            set {
                this.linerateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branch-rate")]
        public string branchrate {
            get {
                return this.branchrateField;
            }
            set {
                this.branchrateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string complexity {
            get {
                return this.complexityField;
            }
            set {
                this.complexityField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class method {

        private line[] linesField;
        
        private string nameField;
        
        private string signatureField;
        
        private string linerateField;
        
        private string branchrateField;
        
        private string complexityField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("line", IsNullable=false)]
        public line[] lines {
            get {
                return this.linesField;
            }
            set {
                this.linesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string signature {
            get {
                return this.signatureField;
            }
            set {
                this.signatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("line-rate")]
        public string linerate {
            get {
                return this.linerateField;
            }
            set {
                this.linerateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("branch-rate")]
        public string branchrate {
            get {
                return this.branchrateField;
            }
            set {
                this.branchrateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string complexity {
            get {
                return this.complexityField;
            }
            set {
                this.complexityField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class line {
        
        private condition[][] conditionsField;
        
        private string numberField;
        
        private string hitsField;
        
        private string branchField;
        
        private string conditioncoverageField;
        
        public line() {
            this.branchField = "false";
            this.conditioncoverageField = "100%";
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("condition", typeof(condition[]), IsNullable=false)]
        public condition[][] conditions {
            get {
                return this.conditionsField;
            }
            set {
                this.conditionsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string number {
            get {
                return this.numberField;
            }
            set {
                this.numberField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string hits {
            get {
                return this.hitsField;
            }
            set {
                this.hitsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute("false")]
        public string branch {
            get {
                return this.branchField;
            }
            set {
                this.branchField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("condition-coverage")]
        [System.ComponentModel.DefaultValueAttribute("100%")]
        public string conditioncoverage {
            get {
                return this.conditioncoverageField;
            }
            set {
                this.conditioncoverageField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class condition {
        
        private string numberField;
        
        private string typeField;
        
        private string coverageField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string number {
            get {
                return this.numberField;
            }
            set {
                this.numberField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string coverage {
            get {
                return this.coverageField;
            }
            set {
                this.coverageField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class sources {
        
        private string[] sourceField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("source")]
        public string[] source {
            get {
                return this.sourceField;
            }
            set {
                this.sourceField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class packages {
        
        private package[] packageField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("package")]
        public package[] package {
            get {
                return this.packageField;
            }
            set {
                this.packageField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class classes {
        
        private @class[] classField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("class")]
        public @class[] @class {
            get {
                return this.classField;
            }
            set {
                this.classField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class methods {
        
        private method[] methodField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("method")]
        public method[] method {
            get {
                return this.methodField;
            }
            set {
                this.methodField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class lines {
        
        private line[] lineField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("line")]
        public line[] line {
            get {
                return this.lineField;
            }
            set {
                this.lineField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://tempuri.org/coverage-04")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://tempuri.org/coverage-04", IsNullable=false)]
    public partial class conditions {
        
        private condition[] conditionField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("condition")]
        public condition[] condition {
            get {
                return this.conditionField;
            }
            set {
                this.conditionField = value;
            }
        }
    }
}
