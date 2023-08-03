using Luban.Core;
using Luban.Core.CodeTarget;
using Luban.Core.DataTarget;
using Luban.Core.Defs;
using Luban.Core.OutputSaver;
using Luban.Core.PostProcess;
using Luban.Core.RawDefs;
using Luban.Core.Schema;

namespace Luban;

public class Pipeline
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly GenerationArguments _genArgs;

    private RawAssembly _rawAssembly;
    
    private DefAssembly _defAssembly;

    private GenerationContext _genCtx;

    public Pipeline(GenerationArguments genArgs)
    {
        _genArgs = genArgs;
    }

    public void Run()
    {
        LoadSchema();
        PrepareGenerationContext();
        ProcessMissions();
    }

    private void LoadSchema()
    {
        string schemaRootFile = _genArgs.GetOption("defaultSchema","rootSchemaFile", true);
        string schemaCollectorName = _genArgs.GetOption("defaultSchema", "schemaCollector", true);
        var schemaCollector = SchemaCollectorFactory.Ins.CreateSchemaCollector(schemaCollectorName);
        schemaCollector.Load(schemaRootFile);
        _rawAssembly = schemaCollector.CreateRawAssembly();
    }

    private void PrepareGenerationContext()
    {
        _defAssembly = new DefAssembly(_rawAssembly);
        _genCtx = new GenerationContext(_defAssembly, _genArgs);
    }

    private void ProcessMissions()
    {
        var tasks = new List<Task>();
        foreach (string mission in _genArgs.CodeTargets)
        {
            ICodeTarget m = CodeTargetManager.Ins.GetCodeTarget(mission);
            tasks.Add(Task.Run(() => ProcessCodeTarget(mission, m)));
        }

        if (_genArgs.DataTargets.Count > 0)
        {
            _genCtx.LoadDatas();
            string dataExporterName = _genCtx.GetOptionOrDefault("global", "dataExporter", true, "default");
            IDataExporter dataExporter = DataTargetManager.Ins.GetDataExporter(dataExporterName);
            foreach (string mission in _genArgs.DataTargets)
            {
                IDataTarget dataTarget = DataTargetManager.Ins.GetTableExporter(mission);
                tasks.Add(Task.Run(() => ProcessDataTarget(mission, dataExporter, dataTarget)));
            }
        }
        Task.WaitAll(tasks.ToArray());
    }

    private void ProcessCodeTarget(string name, ICodeTarget codeTarget)
    {
        var outputManifest = new OutputFileManifest();
        codeTarget.Handle(_genCtx, outputManifest);
        
        if (_genArgs.TryGetOption(name, "postprocess", true, out string postProcessName))
        {
            var oldManifest = outputManifest;
            outputManifest = new OutputFileManifest();
            PostProcessManager.Ins.GetPostProcess(postProcessName).PostProcess(oldManifest, outputManifest);
        }

        string outputSaverName = _genArgs.TryGetOption(name, "outputSaver", true, out string outputSaver)
            ? outputSaver
            : "local";
        var saver = OutputSaverManager.Ins.GetOutputSaver(outputSaverName);
        string outputDir = _genArgs.GetOption($"{CodeTargetBase.FamilyPrefix}.{name}", "outputCodeDir", true);
        saver.Save(outputManifest, outputDir);
    }
    
    private void ProcessDataTarget(string name, IDataExporter mission, IDataTarget dataTarget)
    {
        var outputManifest = new OutputFileManifest();
        mission.Handle(_genCtx, dataTarget, outputManifest);
        
        if (_genArgs.TryGetOption(name, "postprocess", true, out string postProcessName))
        {
            var oldManifest = outputManifest;
            outputManifest = new OutputFileManifest();
            PostProcessManager.Ins.GetPostProcess(postProcessName).PostProcess(oldManifest, outputManifest);
        }

        string outputSaverName = _genArgs.TryGetOption(name, "outputSaver", true, out string outputSaver)
            ? outputSaver
            : "local";
        var saver = OutputSaverManager.Ins.GetOutputSaver(outputSaverName);
        string outputDir = _genArgs.GetOption($"{DataExporterBase.FamilyPrefix}.{name}", "outputDataDir", true);
        saver.Save(outputManifest, outputDir);
    }
}