using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.DM.Builders;

internal class DMCodeTreeBuilder(DMCompiler compiler) {
    private static DMASTFile _astFile = default!;
    private static bool _leftDMStandard;

    private DMCodeTree CodeTree => compiler.DMCodeTree;

    public void BuildCodeTree(DMASTFile astFile) {
        CodeTree.Reset();
        _leftDMStandard = false;
        _astFile = astFile;

        // Add everything in the AST to the code tree
        ProcessFile();

        // Now define everything in the code tree
        CodeTree.DefineEverything();
        if (compiler.Settings.PrintCodeTree)
            CodeTree.Print();

        // Create each types' initialization proc (initializes vars that aren't constants)
        foreach (DMObject dmObject in compiler.DMObjectTree.AllObjects)
            dmObject.CreateInitializationProc();

        // Compile every proc
        foreach (DMProc proc in compiler.DMObjectTree.AllProcs)
            proc.Compile();
    }

    private void ProcessFile() {
        ProcessBlockInner(_astFile.BlockInner, DreamPath.Root);
    }

    private void ProcessBlockInner(DMASTBlockInner blockInner, DreamPath currentType) {
        foreach (DMASTStatement statement in blockInner.Statements) {
            ProcessStatement(statement, currentType);
        }
    }

    private void ProcessStatement(DMASTStatement statement, DreamPath currentType) {
        if (!_leftDMStandard && !statement.Location.InDMStandard) {
            _leftDMStandard = true;
            CodeTree.FinishDMStandard();
        }

        switch (statement) {
            case DMASTObjectDefinition objectDefinition:
                CodeTree.AddType(objectDefinition.Path);
                if (objectDefinition.InnerBlock != null)
                    ProcessBlockInner(objectDefinition.InnerBlock, objectDefinition.Path);
                break;
            case DMASTObjectVarDefinition varDefinition:
                CodeTree.AddType(varDefinition.ObjectPath);
                CodeTree.AddObjectVar(varDefinition.ObjectPath, varDefinition);
                break;
            case DMASTObjectVarOverride varOverride:
                CodeTree.AddType(varOverride.ObjectPath);
                CodeTree.AddObjectVarOverride(varOverride.ObjectPath, varOverride);
                break;
            case DMASTProcDefinition procDefinition:
                var procOwner = currentType.Combine(procDefinition.ObjectPath);

                CodeTree.AddType(procOwner);
                CodeTree.AddProc(procOwner, procDefinition);
                break;
            case DMASTMultipleObjectVarDefinitions multipleVarDefinitions: {
                foreach (DMASTObjectVarDefinition varDefinition in multipleVarDefinitions.VarDefinitions) {
                    CodeTree.AddType(varDefinition.ObjectPath);
                    CodeTree.AddObjectVar(varDefinition.ObjectPath, varDefinition);
                }

                break;
            }
            default:
                compiler.ForcedError(statement.Location, $"Invalid object statement {statement.GetType()}");
                break;
        }
    }
}
