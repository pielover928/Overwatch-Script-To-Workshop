using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : CodeType
    {
        protected override string TypeKindString => "class";
        private Scope ObjectScope { get; }

        public PathmapClass() : base("Pathmap")
        {
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
            Description = "A pathmap can be used for pathfinding.";
            ObjectScope = new Scope("Pathmap");
            ObjectScope.AddMethod(CustomMethodData.GetCustomMethod<Pathfind>(), null, null);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            var objectData = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(actionSet, "_new_PathMap");

            // Get the pathmap data.
            PathMap pathMap = (PathMap)additionalParameterData[0];

            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.NodesAsWorkshopData(), null, 0));
            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.SegmentsAsWorkshopData(), null, 1));

            return objectData.ClassReference.GetVariable();
        }

        public override IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element)
        {
            return translateInfo.SetupClasses().ClassArray.CreateChild((Element)element);
        }

        public override Scope GetObjectScope()
        {
            return ObjectScope;
        }
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Pathmap",
            Kind = CompletionItemKind.Class
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", Extras.GetMarkupContent("File path of the pathmap to use. Must be a `.pathmap` file."))
            };
            Documentation = Extras.GetMarkupContent("Creates a pathmap from a `.pathmap` file.");
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) => throw new NotImplementedException();
    }

    class FileParameter : CodeParameter
    {
        public string FileType { get; }

        /// <summary>
        /// A parameter that resolves to a file. AdditionalParameterData will return the file path.
        /// </summary>
        /// <param name="fileType">The expected file type. Can be null.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="description">The parameter's description. Can be null.</param>
        public FileParameter(string fileType, string parameterName, StringOrMarkupContent description) : base(parameterName, null, null, description)
        {
            FileType = fileType?.ToLower();
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null)
            {
                script.Diagnostics.Error("Expected string constant.", valueRange);
                return null;
            }

            string resultingPath = Extras.CombinePathWithDotNotation(script.Uri.FilePath(), str.Value);
            
            if (resultingPath == null)
            {
                script.Diagnostics.Error("File path contains invalid characters.", valueRange);
                return null;
            }

            string dir = Path.GetDirectoryName(resultingPath);
            if (Directory.Exists(dir))
                DeltinScript.AddImportCompletion(script, dir, valueRange);

            if (!File.Exists(resultingPath))
            {
                script.Diagnostics.Error($"No file was found at '{resultingPath}'.", valueRange);
                return null;
            }

            if (FileType != null && Path.GetExtension(resultingPath).ToLower() != "." + FileType)
            {
                script.Diagnostics.Error($"Expected a file with the file type '{FileType}'.", valueRange);
                return null;
            }

            return resultingPath;
        }
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string file, StringOrMarkupContent description) : base("pathmap", file, description)
        {
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(script, value, valueRange) as string;
            if (filepath == null) return null;

            PathMap map;
            try
            {
                map = PathMap.ImportFromXML(filepath);
            }
            catch (InvalidOperationException)
            {
                script.Diagnostics.Error("Failed to deserialize the PathMap.", valueRange);
                return null;
            }

            return map;
        }
    }

    [CustomMethod("Pathfind", "Pathfinds a player.", CustomMethodType.Action, false)]
    class Pathfind : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("player", "The player to mdve."),
                new CodeParameter("destination", "the destination to move the player to.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            DijkstraNormal algorithm = new DijkstraNormal(
                actionSet, (Element)actionSet.CurrentObject.GetVariable(), Element.Part<V_PositionOf>(parameterValues[0]), (Element)parameterValues[1]
            );
            algorithm.Get();
            DijkstraBase.Pathfind(
                actionSet, actionSet.Translate.DeltinScript.SetupPathfinder(), (Element)algorithm.finalPath.GetVariable(), (Element)parameterValues[0], (Element)parameterValues[1]
            );

            return null;
        }
    }
}