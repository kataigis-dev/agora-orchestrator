using Agora.Configuration;
using Agora.Orchestration;

namespace Agora.Cli;

/// <summary>
/// Interactive, step-by-step builder for an Agora YAML configuration.
/// Drives the user through communication mode, providers, models, agents and
/// (for multi-agent setups) the execution graph, then writes a valid file.
/// Input/output are injected so the flow is unit-testable.
/// </summary>
public static class ConfigWizard
{
    public static int Run(TextReader input, TextWriter output, TextWriter error, string? defaultPath = null)
    {
        try
        {
            var config = Build(input, output);
            return Write(config, input, output, error, defaultPath);
        }
        catch (AbortException)
        {
            error.WriteLine("init: aborted (no more input)");
            return 1;
        }
    }

    private static AgoraConfig Build(TextReader input, TextWriter output)
    {
        output.WriteLine("Agora config wizard — press Enter to accept the [default].");
        output.WriteLine();

        var config = new AgoraConfig
        {
            Communication = Choice(input, output, "Communication mode", new[] { "h2c", "natural" }, "h2c"),
        };
        if (YesNo(input, output, "Handoff mode? (pass only an explicit handoff to the next agent)", false))
            config.Handoff = true;

        ReadProviders(input, output, config);
        ReadModels(input, output, config);

        var modelAliases = config.Models.Keys.ToList();
        config.Defaults.Model = Choice(input, output, "Default model alias", modelAliases, modelAliases[0]);

        ReadSkills(input, output, config);
        ReadMcp(input, output, config);
        ReadRag(input, output, config);
        ReadAgents(input, output, config);
        ReadGraph(input, output, config);

        return config;
    }

    private static void ReadProviders(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        output.WriteLine("Providers — e.g. openai, anthropic, ollama (at least one).");
        while (true)
        {
            var done = config.Providers.Count > 0;
            var name = Ask(input, output, $"  provider name{(done ? " (blank to finish)" : "")}", "");
            if (name.Length == 0)
            {
                if (done) break;
                output.WriteLine("    at least one provider is required.");
                continue;
            }
            var keyEnv = Ask(input, output, "    API key env var (blank if none)", "");
            var baseUrl = Ask(input, output, "    base URL (blank for the provider default)", "");
            config.Providers[name] = new ProviderConfig
            {
                ApiKeyEnv = Blank(keyEnv),
                BaseUrl = Blank(baseUrl),
            };
        }
    }

    private static void ReadModels(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        output.WriteLine("Models — an alias maps to provider + concrete model (at least one).");
        var providers = config.Providers.Keys.ToList();
        while (true)
        {
            var done = config.Models.Count > 0;
            var alias = Ask(input, output, $"  model alias, e.g. fast/balanced{(done ? " (blank to finish)" : "")}", "");
            if (alias.Length == 0)
            {
                if (done) break;
                output.WriteLine("    at least one model is required.");
                continue;
            }
            var provider = Choice(input, output, "    provider", providers, providers[0]);
            var model = Required(input, output, "    model name, e.g. gpt-4o");
            config.Models[alias] = new ModelConfig { Provider = provider, Model = model };
        }
    }

    private static void ReadAgents(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        output.WriteLine("Agents — each is one node in the graph (at least one).");
        var aliases = config.Models.Keys.ToList();
        var defaultModel = config.Defaults.Model!;
        while (true)
        {
            var done = config.Agents.Count > 0;
            var id = Ask(input, output, $"  agent id{(done ? " (blank to finish)" : "")}", "");
            if (id.Length == 0)
            {
                if (done) break;
                output.WriteLine("    at least one agent is required.");
                continue;
            }
            if (config.Agents.ContainsKey(id))
            {
                output.WriteLine($"    agent '{id}' already defined.");
                continue;
            }
            var model = Choice(input, output, "    model alias", aliases, defaultModel);
            var role = Required(input, output, "    role / system prompt");

            var toolsEnabled = config.Skills is not null || config.Mcp is not null
                || config.Rag is not null || config.Handoff == true;
            var skills = config.Skills is not null
                ? AskList(input, output, "    skills (space-separated, blank for none)")
                : new List<string>();
            var tools = toolsEnabled
                ? AskList(input, output, "    tools, e.g. rag_search rag_write ask_agent (blank for none)")
                : new List<string>();
            var approvals = tools.Count > 0
                ? AskSubset(input, output, "    approvals — tools needing human approval (blank for none)", tools)
                : new List<string>();

            config.Agents[id] = new AgentConfig
            {
                Model = model == defaultModel ? null : model,
                Role = role,
                Skills = skills,
                Tools = tools,
                Approvals = approvals,
            };
        }
    }

    private static void ReadSkills(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        if (!YesNo(input, output, "Add skill directories? (reusable SKILL.md prompts)", false))
            return;

        var dirs = new List<string>();
        while (true)
        {
            var dir = Ask(input, output, $"  directory{(dirs.Count > 0 ? " (blank to finish)" : "")}", "");
            if (dir.Length == 0) break;
            dirs.Add(dir);
        }
        if (dirs.Count > 0)
            config.Skills = new SkillsConfig { Directories = dirs };
    }

    private static void ReadMcp(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        if (!YesNo(input, output, "Add MCP tool servers?", false))
            return;

        var mcp = new McpConfig();
        while (true)
        {
            var name = Ask(input, output, $"  server name{(mcp.Servers.Count > 0 ? " (blank to finish)" : "")}", "");
            if (name.Length == 0) break;

            var transport = Choice(input, output, "    transport", new[] { "stdio", "http" }, "stdio");
            var server = new McpServerConfig();
            if (transport == "http")
            {
                server.Url = Required(input, output, "    url");
            }
            else
            {
                server.Command = Required(input, output, "    command, e.g. npx");
                server.Args = AskList(input, output, "    args (space-separated, blank for none)");
            }
            mcp.Servers[name] = server;
        }
        if (mcp.Servers.Count > 0)
            config.Mcp = mcp;
    }

    private static void ReadRag(TextReader input, TextWriter output, AgoraConfig config)
    {
        output.WriteLine();
        if (!YesNo(input, output, "Enable RAG (retrieval-augmented generation)?", false))
            return;

        output.WriteLine("  Using the built-in 'fake' embedder (real embeddings need an IEmbedder injected in code).");

        var store = new VectorStoreConfig
        {
            Type = Choice(input, output, "  vector store", new[] { "memory", "file" }, "memory"),
        };
        if (store.Type == "file")
            store.Path = Required(input, output, "    store path, e.g. ./kb.json");

        var ingest = new IngestConfig
        {
            Sources = AskList(input, output, "  ingest sources — file/dir paths (space-separated)"),
            ChunkSize = AskInt(input, output, "  chunk size", 800),
            ChunkOverlap = AskInt(input, output, "  chunk overlap", 120),
        };
        var topK = AskInt(input, output, "  top_k (chunks retrieved per query)", 6);

        var refine = new RefineConfig
        {
            Strategy = Choice(input, output, "  refine strategy", new[] { "none", "llm" }, "none"),
        };
        if (refine.Strategy == "llm")
            refine.Model = Choice(input, output, "    refine model alias",
                config.Models.Keys.ToList(), config.Defaults.Model!);

        config.Rag = new RagConfig
        {
            Enabled = true,
            Retrieval = new RetrievalConfig
            {
                Embedder = new EmbedderConfig { Type = "fake" },
                VectorStore = store,
                TopK = topK,
            },
            Ingest = ingest,
            Refine = refine,
        };
    }

    private static void ReadGraph(TextReader input, TextWriter output, AgoraConfig config)
    {
        if (config.Agents.Count < 2)
            return;

        output.WriteLine();
        if (!YesNo(input, output, "Configure a multi-agent execution graph?", true))
            return;

        var agentIds = config.Agents.Keys.ToList();
        var graph = new GraphConfig
        {
            Entry = Choice(input, output, "  entry agent", agentIds, agentIds[0]),
        };

        var targets = agentIds.Append(Graph.End).ToList();
        output.WriteLine("  Edges — connect agents; target END to stop (at least one).");
        while (true)
        {
            var done = graph.Edges.Count > 0;
            var from = Choice(input, output, $"    from{(done ? " (blank to finish)" : "")}",
                agentIds, done ? "" : agentIds[0], allowBlank: done);
            if (from.Length == 0) break;

            var to = Choice(input, output, "    to", targets, Graph.End);
            var type = Choice(input, output, "    type", new[] { "sequential", "handoff", "conditional" }, "sequential");

            string? when = null;
            int? maxLoops = null;
            if (type == "conditional")
            {
                when = Required(input, output, "      when (signal name)");
                var raw = Ask(input, output, "      max_loops (blank for none)", "");
                if (int.TryParse(raw, out var n) && n > 0)
                    maxLoops = n;
            }

            graph.Edges.Add(new GraphEdgeConfig { From = from, To = to, Type = type, When = when, MaxLoops = maxLoops });
        }

        config.Graph = graph;
    }

    private static int Write(AgoraConfig config, TextReader input, TextWriter output, TextWriter error, string? defaultPath)
    {
        output.WriteLine();
        var path = Ask(input, output, "Write config to", defaultPath ?? "./agora.yaml");
        if (File.Exists(path) && !YesNo(input, output, $"{path} exists — overwrite?", false))
        {
            error.WriteLine("init: aborted (file exists)");
            return 1;
        }

        ConfigWriter.Save(config, path);

        try
        {
            ConfigLoader.Load(path);
        }
        catch (ConfigException e)
        {
            error.WriteLine($"init: generated config is invalid: {e.Message}");
            return 1;
        }

        output.WriteLine();
        output.WriteLine($"wrote {path}");
        output.WriteLine($"  validate: agora validate --config {path}");
        if (config.Rag is not null)
            output.WriteLine($"  ingest:   agora ingest --config {path}");
        output.WriteLine(config.Graph is null
            ? $"  run:      agora run --config {path} --agent {config.Agents.Keys.First()} --input \"...\""
            : $"  run:      agora run --config {path} --input \"...\" --graph");
        return 0;
    }

    // --- prompt helpers ---------------------------------------------------

    private static string Ask(TextReader input, TextWriter output, string prompt, string @default)
    {
        var suffix = @default.Length > 0 ? $" [{@default}]" : "";
        output.Write($"{prompt}{suffix}: ");
        var line = ReadLine(input);
        return line.Length == 0 ? @default : line;
    }

    private static string Required(TextReader input, TextWriter output, string prompt)
    {
        while (true)
        {
            var value = Ask(input, output, prompt, "");
            if (value.Length > 0) return value;
            output.WriteLine("    a value is required.");
        }
    }

    private static List<string> AskList(TextReader input, TextWriter output, string prompt)
        => Ask(input, output, prompt, "")
            .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static List<string> AskSubset(TextReader input, TextWriter output, string prompt,
        IReadOnlyList<string> allowed)
    {
        while (true)
        {
            var values = AskList(input, output, prompt);
            var invalid = values.Where(v => !allowed.Contains(v)).ToList();
            if (invalid.Count == 0) return values;
            output.WriteLine($"    not in tools: {string.Join(", ", invalid)}");
        }
    }

    private static int AskInt(TextReader input, TextWriter output, string prompt, int @default)
    {
        while (true)
        {
            var raw = Ask(input, output, prompt, @default.ToString());
            if (int.TryParse(raw, out var n) && n >= 0) return n;
            output.WriteLine("    enter a non-negative integer.");
        }
    }

    private static string Choice(TextReader input, TextWriter output, string prompt,
        IReadOnlyList<string> options, string @default, bool allowBlank = false)
    {
        var rendered = $"{prompt} [{string.Join("/", options)}]";
        while (true)
        {
            var value = Ask(input, output, rendered, @default);
            if (value.Length == 0 && allowBlank) return "";
            if (options.Contains(value)) return value;
            output.WriteLine($"    choose one of: {string.Join(", ", options)}");
        }
    }

    private static bool YesNo(TextReader input, TextWriter output, string prompt, bool defaultYes)
    {
        var value = Ask(input, output, $"{prompt} [{(defaultYes ? "Y/n" : "y/N")}]", defaultYes ? "y" : "n");
        return value.StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadLine(TextReader input)
    {
        var line = input.ReadLine();
        if (line is null) throw new AbortException();
        return line.Trim();
    }

    private static string? Blank(string value) => value.Length == 0 ? null : value;

    private sealed class AbortException : Exception;
}
