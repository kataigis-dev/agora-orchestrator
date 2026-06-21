# 01 — Foundations: LLMs and prompting

Before talking about agents we need a shared vocabulary about **large language models** (LLMs), the
building block everything else is built on.

## What an LLM is

An LLM is a neural-network model trained on enormous amounts of text to **predict the next token** given
a sequence of preceding tokens. From this apparently simple capability complex behaviors emerge:
answering questions, summarizing, translating, writing code, reasoning step by step. An LLM does not
"consult a database" when it answers: it generates plausible text based on the statistical patterns
learned during training. This has two fundamental consequences:

1. **Frozen knowledge.** The model only knows what was in its training data up to a certain date
   (*knowledge cutoff*). It does not know later events nor private/company data. This is the problem
   that [RAG](05-rag.md) solves.
2. **Hallucinations.** The model can produce fluent but false statements, because it optimizes for
   linguistic plausibility, not truth. This is why serious systems **verify** outputs instead of
   trusting them (see [evaluation](10-evaluation-observability.md) and
   [spec-driven development](09-spec-driven-development.md)).

## Tokens and the context window

- **Token**: the unit the model reads and writes with. A token is roughly 3–4 characters in English (a
  bit more in Italian); "orchestrazione" may be worth several tokens. The cost and latency of a call
  depend on the number of input and output tokens.
- **Context window**: the maximum number of tokens the model can consider in a single call (prompt +
  response). It is a **finite resource with diminishing returns**: beyond a certain amount, adding text
  degrades quality instead of improving it. Managing this space well is the topic of
  [context engineering](06-memory-context.md).

## The prompt

The **prompt** is the input text. It is usually broken down into:

- **System prompt**: defines the assistant's role, behavior, constraints and response format. It
  persists for the whole conversation.
- **User / assistant messages**: the dialogue itself.
- **Additional context**: retrieved documents (RAG), tool results, memory.

Anthropic recommends a precise ordering of the context — **system instructions first, then relevant
memory, then tool definitions, finally the conversation history** — because "the arrangement and quality
of this context determine the agent's performance more than any other factor"
([Anthropic, *Effective context engineering*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).

### Common prompting techniques

| Technique | Idea |
|-----------|------|
| *Zero-shot* | You ask directly, with no examples |
| *Few-shot* | You provide a few examples of the desired behavior |
| *Chain-of-thought* | You ask the model to "reason step by step" before answering |
| *Structured output* | You constrain the answer to a format (e.g. JSON), making it machine-verifiable |

Structured output is especially important in agentic systems: constraining the model to JSON "removes
ambiguity and allows for more standardized evaluation"
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).

## Generation parameters

- **Temperature**: controls randomness. Low values (e.g. 0–0.3) make responses more deterministic and
  repeatable; high values increase variety/creativity. For precision tasks (extraction, classification,
  routing) low values are preferred.
- **Max tokens**: a limit on the response length.
- **Top-p / top-k**: alternative token-sampling strategies.

## Embeddings

An **embedding** is the representation of a piece of text as a **numeric vector** that captures its
semantic meaning: similar texts have nearby vectors in the space. Embeddings are the foundation of
semantic search and therefore of [RAG](05-rag.md): you compare the query's vector with the documents'
vectors to find the most relevant ones, even when the words do not match exactly
([AWS, *What is RAG?*](https://aws.amazon.com/what-is/retrieval-augmented-generation/)).

## Non-determinism: the architectural challenge

Unlike traditional software, an LLM's decisions are **inherently non-deterministic**: the same input can
produce different outputs. AWS lists this as one of the dimensions that agentic design must address
explicitly
([AWS, *Agentic AI — Generative AI Lens*](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html)).
The practical consequence, echoed throughout the following chapters: **don't trust the model's narration;
verify with deterministic checks** whenever possible.

---

Next: [02 — Agents](02-agents.md).
