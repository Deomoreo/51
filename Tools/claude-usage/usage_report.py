"""Token usage report from Claude Code transcripts for this project.

Usage: python usage_report.py [--cutoff YYYY-MM-DD] [--save-baseline]
Sessions starting before the cutoff are the "before" group; the rest "after".
Only per-call averages are compared (sessions differ in length).
"""
import argparse, collections, glob, json, os, statistics

ROOT = os.path.join(os.path.expanduser("~"), ".claude", "projects", "C--Users-franc-Documents-GitHub-51")
HERE = os.path.dirname(os.path.abspath(__file__))


def load_sessions():
    out = []
    for f in glob.glob(os.path.join(ROOT, "*.jsonl")):
        seen, first = set(), None
        t = collections.Counter()
        models = collections.Counter()
        for line in open(f, encoding="utf-8", errors="ignore"):
            try:
                o = json.loads(line)
            except ValueError:
                continue
            if o.get("timestamp") and not first:
                first = o["timestamp"]
            m = o.get("message")
            if o.get("type") != "assistant" or not isinstance(m, dict) or not m.get("usage"):
                continue
            k = m.get("id") or o.get("uuid")
            if k in seen or m.get("model") == "<synthetic>":
                continue
            seen.add(k)
            u = m["usage"]
            ctx = u.get("input_tokens", 0) + u.get("cache_read_input_tokens", 0) + u.get("cache_creation_input_tokens", 0)
            t["calls"] += 1
            t["out"] += u.get("output_tokens", 0)
            t["cread"] += u.get("cache_read_input_tokens", 0)
            t["cwrite"] += u.get("cache_creation_input_tokens", 0)
            t["ctx"] += ctx
            t["maxctx"] = max(t["maxctx"], ctx)
            models[m.get("model", "?")] += 1
        if t["calls"]:
            out.append({"id": os.path.basename(f)[:8], "start": first or "", "models": dict(models), **t})
    return sorted(out, key=lambda s: s["start"])


def summarize(sessions):
    calls = sum(s["calls"] for s in sessions)
    if not calls:
        return None
    opus = sum(n for s in sessions for m, n in s["models"].items() if "opus" in m)
    return {
        "sessions": len(sessions),
        "calls": calls,
        "avg_context_per_call": sum(s["ctx"] for s in sessions) // calls,
        "cache_read_per_call": sum(s["cread"] for s in sessions) // calls,
        "output_per_call": sum(s["out"] for s in sessions) // calls,
        "opus_share_pct": round(100 * opus / calls, 1),
        "median_session_peak_ctx": int(statistics.median(s["maxctx"] for s in sessions)),
        "total_cache_read": sum(s["cread"] for s in sessions),
        "total_output": sum(s["out"] for s in sessions),
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cutoff", default="2026-09-21")
    ap.add_argument("--save-baseline", action="store_true")
    a = ap.parse_args()
    ss = load_sessions()
    before = [s for s in ss if s["start"][:10] < a.cutoff]
    after = [s for s in ss if s["start"][:10] >= a.cutoff]
    b, n = summarize(before), summarize(after)
    if a.save_baseline:
        json.dump(b, open(os.path.join(HERE, "baseline.json"), "w"), indent=2)
        print("baseline saved")
    print(f"{'metric':28s}{'before':>16s}{'after':>16s}{'change':>10s}")
    for k in (b or {}):
        bv, nv = b[k], (n or {}).get(k)
        ch = f"{100*(nv-bv)/bv:+.0f}%" if nv is not None and bv else "-"
        print(f"{k:28s}{bv:>16,}{(nv if nv is not None else '-'):>16,}{ch:>10s}" if nv is not None else f"{k:28s}{bv:>16,}{'-':>16s}{'-':>10s}")
    if not n:
        print("\nNo sessions after the cutoff yet.")


if __name__ == "__main__":
    main()
