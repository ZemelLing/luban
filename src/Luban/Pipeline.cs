using Luban.Core;
using Luban.Core.CodeGeneration;
using Luban.Core.Defs;
using Luban.Core.Mission;
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
        foreach (string mission in _genArgs.Missions)
        {
            IMission m = MissionManager.Ins.GetMission(mission);
            tasks.Add(Task.Run(() => ProcessMission(mission, m)));
        }
        Task.WaitAll(tasks.ToArray());
    }

    private void ProcessMission(string name, IMission mission)
    {
        var outputManifest = new OutputFileManifest();
        mission.Handle(_genCtx, outputManifest);
        

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
        saver.Save(outputManifest);
    }
}