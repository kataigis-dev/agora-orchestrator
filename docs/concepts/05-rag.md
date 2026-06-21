# 05 — RAG: Retrieval-Augmented Generation

## The problem it solves

An LLM only knows its training data up to its *knowledge cutoff* and cannot see an organization's private
or up-to-date data (see [01](01-llm-foundations.md)). Retraining the model is expensive and slow. **RAG**
is the technique for giving the model access to an **authoritative external knowledge base** *at
generation time*, without changing its weights.

> "RAG is the process of optimizing the output of an LLM so it references an authoritative knowledge
> base, outside its training data sources, before generating a response."
> — [AWS, *What is RAG?*](https://aws.amazon.com/what-is/retrieval-augmented-generation/)

## How it works (high level)

When the user asks a question
([AWS](https://aws.amazon.com/what-is/retrieval-augmented-generation/),
[NVIDIA](https://blogs.nvidia.com/blog/what-is-retrieval-augmented-generation/)):

1. the question is converted into an **embedding** (a numeric vector);
2. the **vector store** is searched for the chunks whose embedding is closest to the question's — this is
   **semantic search**;
3. the retrieved chunks are **injected into the prompt** together with the question;
4. the LLM generates the answer combining its own linguistic ability with the retrieved data, and can
   **cite the sources**.

The result: up-to-date answers, grounded in verifiable sources, with fewer hallucinations.

## The RAG pipeline in detail

### Indexing phase (offline / *ingest*)

```
documents ──▶ chunking ──▶ embedding ──▶ vector store
```

- **Chunking**: splitting large documents into smaller, manageable **chunks**. The chunk size and the
  **overlap** between chunks are key parameters: chunks too large dilute relevance, too small lose
  context ([AWS](https://aws.amazon.com/what-is/retrieval-augmented-generation/)).
- **Embedding**: turning each chunk into its numeric vector, which captures its semantic meaning.
- **Vector store** (or *vector database*/*vector index*): the database that stores the embeddings and
  enables efficient nearest-neighbor searches. Examples: in-memory indexes, on file, or dedicated
  services (Qdrant, etc.).

### Query phase (online / *retrieval + generation*)

```
question ──▶ embedding ──▶ search (top-k) ──▶ [rerank] ──▶ prompt + context ──▶ LLM ──▶ answer
```

- **Top-k**: how many chunks to retrieve.
- **Score threshold**: the minimum similarity to include a chunk.
- **Reranking** (optional): a second model reorders the results by relevance before passing them to the
  LLM, improving precision.

## RAG variants

| Variant | Idea |
|---------|------|
| **Naive RAG** | The base pipeline described above: retrieve *k* chunks and generate |
| **Advanced RAG** | Adds query pre-processing, reranking, fusion of multiple sources, rewriting |
| **Agentic RAG** | *Retrieval* becomes a **tool** the agent decides when and how to use, possibly iterating multiple searches and reasoning over the results |

**Agentic RAG** is the point of contact with the agent chapters: instead of a fixed pipeline, retrieval
is a capability the agent invokes dynamically within its reasoning loop.

## RAG vs fine-tuning vs context window

- **RAG**: for **factual, dynamic, private** knowledge. Updatable by changing the documents, without
  retraining. Cites the sources.
- **Fine-tuning**: to teach **style, format or skills**, not facts that change often.
- **Long context**: if the relevant documents are few and small, sometimes it is enough to put them
  directly into the prompt. But the context window has diminishing returns (see
  [06](06-memory-context.md)), so RAG remains preferable over large corpora.

## Evaluating a RAG system

A RAG system must be evaluated on two distinct fronts:

- **Retrieval quality**: are the retrieved chunks relevant? (precision/recall metrics over retrieval).
- **Generation quality**: is the answer *faithful* to the retrieved chunks and *relevant* to the
  question? Here automatic judges are often used (see [10 — Evaluation](10-evaluation-observability.md)).

## RAG as a *writable* knowledge base

In multi-agent systems the vector store is not only for reading: it can become a **writable shared
memory**, in which agents deposit intermediate results that other agents retrieve. This connects RAG to
the topics of [memory](06-memory-context.md) and
[orchestration](08-multi-agent-orchestration.md).

---

Previous: [04 — MCP](04-mcp.md) · Next: [06 — Memory and context engineering](06-memory-context.md).
