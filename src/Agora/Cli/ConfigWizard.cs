using Agora.Configuration;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;

namespace Agora.Cli;

/// <summary>
/// Interactive, step-by-step builder for an Agora YAML configuration.
/// Drives the user through communication mode, providers, models, agents and
/// (for multi-agent setups) the execution graph, then writes a valid file.
/// Prompting is delegated to a <see cref="Prompter"/> over the injected I/O so the flow is testable.
/// </summary>
public static class ConfigWizard
{
    /// <summary>Runs the wizard against the given I/O, building and writing the config; returns the
    /// exit code (1 if aborted or the file already exists and the user declines to overwrite).</summary>
    public static int Run(TextReader input, TextWriter output, TextWriter error, string? defaultPath = null)
    {
        var prompter = new Prompter(input, output);
        try
        {
            var config = Build(prompter);
            return Write(config, prompter, error, defaultPath);
        }
        catch (PrompterAbortException)
        {
            error.WriteLine("init: aborted (no more input)");
            return 1;
        }
    }

    /// <summary>Drives all the prompts in order and assembles the resulting <see cref="AgoraConfig"/>.</summary>
    private static AgoraConfig Build(Prompter p)
    {
        p.WriteLine("Agora config wizard — press Enter to accept the [default].");
        p.WriteLine();

        var config = new AgoraConfig
        {
            Communication = p.Choice("Communication mode", new[] { "h2c", "natural" }, "h2c"),
        };
        if (p.YesNo("Handoff mode? (pass only an explicit handoff to the next agent)", false))
            config.Handoff = true;

        ReadProviders(p, config);
        ReadModels(p, config);

        var modelAliases = config.Models.Keys.ToList();
        config.Defaults.Model = p.Choice("Default model alias", modelAliases, modelAliases[0]);

        ReadSkills(p, config);
        ReadMcp(p, config);
        ReadRag(p, config);
        ReadAgents(p, config);
        ReadGraph(p, config);

        return config;
    }

    /// <summary>Prompts for one or more providers (name + optional key env/base URL).</summary>
    private static void ReadProviders(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        p.WriteLine("Providers — e.g. openai, ollama, github-copilot (at least one).");
        while (true)
        {
            var done = config.Providers.Count > 0;
            var name = p.Ask($"  provider name{(done ? " (blank to finish)" : "")}");
            if (name.Length == 0)
            {
                if (done) break;
                p.WriteLine("    at least one provider is required.");
                continue;
            }
            var keyEnv = p.Ask("    API key env var (blank if none)");
            var baseUrl = p.Ask("    base URL (blank for the provider default)");
            config.Providers[name] = new ProviderConfig
            {
                ApiKeyEnv = Blank(keyEnv),
                BaseUrl = Blank(baseUrl),
            };
        }
    }

    /// <summary>Prompts for one or more model aliases (alias → provider + concrete model).</summary>
    private static void ReadModels(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        p.WriteLine("Models — an alias maps to provider + concrete model (at least one).");
        var providers = config.Providers.Keys.ToList();
        while (true)
        {
            var done = config.Models.Count > 0;
            var alias = p.Ask($"  model alias, e.g. fast/balanced{(done ? " (blank to finish)" : "")}");
            if (alias.Length == 0)
            {
                if (done) break;
                p.WriteLine("    at least one model is required.");
                continue;
            }
            var provider = p.Choice("    provider", providers, providers[0]);
            var model = p.Required("    model name, e.g. gpt-4o");
            config.Models[alias] = new ModelConfig { Provider = provider, Model = model };
        }
    }

    /// <summary>Prompts for one or more agents (id, model, role, and—when enabled—skills/tools/approvals).</summary>
    private static void ReadAgents(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        p.WriteLine("Agents — each is one node in the graph (at least one).");
        var aliases = config.Models.Keys.ToList();
        var defaultModel = config.Defaults.Model!;
        while (true)
        {
            var done = config.Agents.Count > 0;
            var id = p.Ask($"  agent id{(done ? " (blank to finish)" : "")}");
            if (id.Length == 0)
            {
                if (done) break;
                p.WriteLine("    at least one agent is required.");
                continue;
            }
            if (config.Agents.ContainsKey(id))
            {
                p.WriteLine($"    agent '{id}' already defined.");
                continue;
            }
            var model = p.Choice("    model alias", aliases, defaultModel);
            var role = p.Required("    role / system prompt");

            var toolsEnabled = config.Skills is not null || config.Mcp is not null
                || config.Rag is not null || config.Handoff == true;
            var skills = config.Skills is not null
                ? p.AskList("    skills (space-separated, blank for none)")
                : new List<string>();
            var tools = toolsEnabled
                ? p.AskList("    tools, e.g. rag_search rag_write ask_agent (blank for none)")
                : new List<string>();
            var approvals = tools.Count > 0
                ? p.AskSubset("    approvals — tools needing human approval (blank for none)", tools)
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

    /// <summary>Optionally prompts for skill directories.</summary>
    private static void ReadSkills(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        if (!p.YesNo("Add skill directories? (reusable SKILL.md prompts)", false))
            return;

        var dirs = new List<string>();
        while (true)
        {
            var dir = p.Ask($"  directory{(dirs.Count > 0 ? " (blank to finish)" : "")}");
            if (dir.Length == 0) break;
            dirs.Add(dir);
        }
        if (dirs.Count > 0)
            config.Skills = new SkillsConfig { Directories = dirs };
    }

    /// <summary>Optionally prompts for MCP tool servers (stdio or http).</summary>
    private static void ReadMcp(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        if (!p.YesNo("Add MCP tool servers?", false))
            return;

        var mcp = new McpConfig();
        while (true)
        {
            var name = p.Ask($"  server name{(mcp.Servers.Count > 0 ? " (blank to finish)" : "")}");
            if (name.Length == 0) break;

            var transport = p.Choice("    transport", new[] { "stdio", "http" }, "stdio");
            var server = new McpServerConfig();
            if (transport == "http")
            {
                server.Url = p.Required("    url");
            }
            else
            {
                server.Command = p.Required("    command, e.g. npx");
                server.Args = p.AskList("    args (space-separated, blank for none)");
            }
            mcp.Servers[name] = server;
        }
        if (mcp.Servers.Count > 0)
            config.Mcp = mcp;
    }

    /// <summary>Optionally prompts for the RAG pipeline (store, ingest sources, chunking, top_k, refine).</summary>
    private static void ReadRag(Prompter p, AgoraConfig config)
    {
        p.WriteLine();
        if (!p.YesNo("Enable RAG (retrieval-augmented generation)?", false))
            return;

        // A writable KB (rag_write) needs a real, semantic embedder so the conflict check works — the
        // offline 'fake' embedder is rejected for writable KBs (see RagWriteValidator). A read-only KB
        // may use 'fake'.
        EmbedderConfig embedder;
        if (p.YesNo("  Will agents write to the KB (rag_write)?", false))
        {
            var type = p.Choice("  embedder", new[] { "openai", "ollama" }, "openai");
            embedder = new EmbedderConfig
            {
                Type = type,
                Model = p.Required("    embedding model, e.g. text-embedding-3-small / nomic-embed-text"),
            };
            p.WriteLine($"  Ensure a '{type}' provider is configured (api key / base_url).");
        }
        else
        {
            p.WriteLine("  Using the built-in 'fake' embedder (read-only KB).");
            embedder = new EmbedderConfig { Type = "fake" };
        }

        var store = new VectorStoreConfig
        {
            Type = p.Choice("  vector store", new[] { "memory", "file" }, "memory"),
        };
        if (store.Type == "file")
            store.Path = p.Required("    store path, e.g. ./kb.json");

        var ingest = new IngestConfig
        {
            Sources = p.AskList("  ingest sources — file/dir paths (space-separated)"),
            ChunkSize = p.AskInt("  chunk size", 800),
            ChunkOverlap = p.AskInt("  chunk overlap", 120),
        };
        var topK = p.AskInt("  top_k (chunks retrieved per query)", 6);

        var refine = new RefineConfig
        {
            Strategy = p.Choice("  refine strategy", new[] { "none", "llm" }, "none"),
        };
        if (refine.Strategy == "llm")
            refine.Model = p.Choice("    refine model alias",
                config.Models.Keys.ToList(), config.Defaults.Model!);

        config.Rag = new RagConfig
        {
            Enabled = true,
            Retrieval = new RetrievalConfig
            {
                Embedder = embedder,
                VectorStore = store,
                TopK = topK,
            },
            Ingest = ingest,
            Refine = refine,
        };
    }

    /// <summary>For multi-agent configs, optionally prompts for the entry node and edges.</summary>
    private static void ReadGraph(Prompter p, AgoraConfig config)
    {
        if (config.Agents.Count < 2)
            return;

        p.WriteLine();
        if (!p.YesNo("Configure a multi-agent execution graph?", true))
            return;

        var agentIds = config.Agents.Keys.ToList();
        var graph = new GraphConfig
        {
            Entry = p.Choice("  entry agent", agentIds, agentIds[0]),
        };

        var targets = agentIds.Append(Graph.End).ToList();
        p.WriteLine("  Edges — connect agents; target END to stop (at least one).");
        while (true)
        {
            var done = graph.Edges.Count > 0;
            var from = p.Choice($"    from{(done ? " (blank to finish)" : "")}",
                agentIds, done ? "" : agentIds[0], allowBlank: done);
            if (from.Length == 0) break;

            var to = p.Choice("    to", targets, Graph.End);
            var type = p.Choice("    type", new[] { "sequential", "handoff", "conditional" }, "sequential");

            string? when = null;
            int? maxLoops = null;
            if (type == "conditional")
            {
                when = p.Required("      when (signal name)");
                var raw = p.Ask("      max_loops (blank for none)");
                if (int.TryParse(raw, out var n) && n > 0)
                    maxLoops = n;
            }

            graph.Edges.Add(new GraphEdgeConfig { From = from, To = to, Type = type, When = when, MaxLoops = maxLoops });
        }

        config.Graph = graph;
    }

    /// <summary>Writes the config to the chosen path (confirming overwrite), re-validates it, and
    /// prints next-step commands.</summary>
    private static int Write(AgoraConfig config, Prompter p, TextWriter error, string? defaultPath)
    {
        p.WriteLine();
        var path = p.Ask("Write config to", defaultPath ?? "./agora.yaml");
        if (File.Exists(path) && !p.YesNo($"{path} exists — overwrite?", false))
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

        p.WriteLine();
        p.WriteLine($"wrote {path}");
        p.WriteLine($"  validate: agora validate --config {path}");
        if (config.Rag is not null)
            p.WriteLine($"  ingest:   agora ingest --config {path}");
        p.WriteLine(config.Graph is null
            ? $"  run:      agora run --config {path} --agent {config.Agents.Keys.First()} --input \"...\""
            : $"  run:      agora run --config {path} --input \"...\" --graph");
        return 0;
    }

    /// <summary>Maps an empty string to null (so omitted optional fields are not serialized).</summary>
    private static string? Blank(string value) => value.Length == 0 ? null : value;
}
