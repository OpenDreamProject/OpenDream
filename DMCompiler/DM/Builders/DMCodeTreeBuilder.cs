using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.DM.Builders;

internal static class DMCodeTreeBuilder {
    private static DMASTFile _astFile = default!;
    private static bool _leftDMStandard;

    public static void BuildCodeTree(DMASTFile astFile) {
        DMCodeTree.Reset();
        _leftDMStandard = false;
        _astFile = astFile;

        // Add everything in the AST to the code tree
        ProcessFile();

        // Now define everything in the code tree
        DMCodeTree.DefineEverything();
        if (DMCompiler.Settings.PrintCodeTree)
            DMCodeTree.Print();

        // Create each types' initialization proc (initializes vars that aren't constants)
        foreach (DMObject dmObject in DMObjectTree.AllObjects)
            dmObject.CreateInitializationProc();

        // Compile every proc
        foreach (DMProc proc in DMObjectTree.AllProcs)
            proc.Compile();
    }

    private static void ProcessFile() {
        ProcessBlockInner(_astFile.BlockInner, DreamPath.Root);
    }

    private static void ProcessBlockInner(DMASTBlockInner blockInner, DreamPath currentType) {
        foreach (DMASTStatement statement in blockInner.Statements) {
            ProcessStatement(statement, currentType);
        }
    }

    private static void ProcessStatement(DMASTStatement statement, DreamPath currentType) {
        if (!_leftDMStandard && !statement.Location.InDMStandard) {
            _leftDMStandard = true;
            DMCodeTree.FinishDMStandard();
        }

        switch (statement) {
            case DMASTObjectDefinition objectDefinition:
                DMCodeTree.AddType(objectDefinition.Path);
                if (objectDefinition.InnerBlock != null)
                    ProcessBlockInner(objectDefinition.InnerBlock, objectDefinition.Path);
                break;
            case DMASTObjectVarDefinition varDefinition:
                DMCodeTree.AddType(varDefinition.ObjectPath);
                DMCodeTree.AddObjectVar(varDefinition.ObjectPath, varDefinition);
                break;
            case DMASTObjectVarOverride varOverride:
                DMCodeTree.AddType(varOverride.ObjectPath);
                DMCodeTree.AddObjectVarOverride(varOverride.ObjectPath, varOverride);
                break;
            case DMASTProcDefinition procDefinition:
                var procOwner = currentType.Combine(procDefinition.ObjectPath);

                DMCodeTree.AddType(procOwner);
                DMCodeTree.AddProc(procOwner, procDefinition);
                break;
            case DMASTMultipleObjectVarDefinitions multipleVarDefinitions: {
                foreach (DMASTObjectVarDefinition varDefinition in multipleVarDefinitions.VarDefinitions) {
                    DMCodeTree.AddType(varDefinition.ObjectPath);
                    DMCodeTree.AddObjectVar(varDefinition.ObjectPath, varDefinition);
                }

                break;
            }
            default:
                DMCompiler.ForcedError(statement.Location, $"Invalid object statement {statement.GetType()}");
                break;
        }
    }
}
