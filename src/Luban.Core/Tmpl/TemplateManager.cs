﻿using System.Collections.Concurrent;
using System.Text;
using Scriban;

namespace Luban.Core.Tmpl;

public class TemplateManager
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    
    public static TemplateManager Ins { get; } = new();


    private readonly List<string> _templateSearchPaths = new();

    private readonly ConcurrentDictionary<string, Template> _templates = new();

    private readonly ConcurrentDictionary<string, string> _templateStrings = new();

    public void Init()
    {
    }

    public void AddTemplateSearchPath(string templateSearchPath, bool sureExists = false)
    {
        if (sureExists && !Directory.Exists(templateSearchPath))
        {
            s_logger.Error("template search path:{} not exists", templateSearchPath);
            return;
        }
        _templateSearchPaths.Add(templateSearchPath);
    }

    public bool TryGetTemplateString(string templateName, out string result)
    {
        if (_templateStrings.TryGetValue(templateName, out result))
        {
            return true;
        }
        foreach (var searchPath in _templateSearchPaths)
        {
            var fullPath = $"{searchPath}/{templateName}.tpl";
            if (File.Exists(fullPath))
            {
                result = File.ReadAllText(fullPath, Encoding.UTF8);
                _templateStrings[templateName] = result;
                return true;
            }
        }
        result = null;
        return false;
    }

    public string GetTemplateString(string templateName)
    {
        if (TryGetTemplateString(templateName, out var strTpl))
        {
            return strTpl;
        }
        throw new FileNotFoundException($"can't find {templateName}.tpl in paths:{string.Join(';', _templateSearchPaths)}");
    }

    public Scriban.Template GetTemplate(string templateName)
    {
        return _templates.GetOrAdd(templateName, tn => Template.Parse(GetTemplateString(tn)));
    }

    public bool TryGetTemplate(string templateName, out Template template)
    {
        if (_templates.TryGetValue(templateName, out template))
        {
            return true;
        }
        if (TryGetTemplateString(templateName, out var strTpm))
        {
            template = _templates.GetOrAdd(templateName, tn => Template.Parse(strTpm));
            return true;
        }
        template = null;
        return false;
    }

    public Template GetOrAddTemplate(string templateName, Func<string, Template> creator)
    {
        return _templates.GetOrAdd(templateName, creator);
    }
}