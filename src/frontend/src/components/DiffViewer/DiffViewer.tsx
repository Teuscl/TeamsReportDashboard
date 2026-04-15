import { useMemo, useRef, useState } from "react";
import { diffLines } from "diff";
import { cn } from "@/lib/utils";
import { Columns2, AlignJustify } from "lucide-react";

// ─── Types ──────────────────────────────────────────────────────────────────

interface DiffLine {
  content: string;
  type: "same" | "added" | "removed" | "empty";
  lineNumber: number | null;
}

interface SideBySide {
  left: DiffLine[];
  right: DiffLine[];
}

interface UnifiedLine {
  content: string;
  type: "same" | "added" | "removed";
  leftNum: number | null;
  rightNum: number | null;
}

type ViewMode = "split" | "unified";

// ─── Helpers ─────────────────────────────────────────────────────────────────

function splitLines(value: string): string[] {
  const lines = value.split("\n");
  if (lines[lines.length - 1] === "") lines.pop();
  return lines;
}

function buildSideBySide(oldText: string, newText: string): SideBySide {
  const changes = diffLines(oldText, newText);
  const left: DiffLine[] = [];
  const right: DiffLine[] = [];
  let leftNum = 1;
  let rightNum = 1;

  let i = 0;
  while (i < changes.length) {
    const change = changes[i];

    if (!change.added && !change.removed) {
      for (const line of splitLines(change.value)) {
        left.push({ content: line, type: "same", lineNumber: leftNum++ });
        right.push({ content: line, type: "same", lineNumber: rightNum++ });
      }
      i++;
    } else if (change.removed) {
      const nextChange = changes[i + 1];
      const removedLines = splitLines(change.value);
      const addedLines = nextChange?.added ? splitLines(nextChange.value) : [];
      const maxLen = Math.max(removedLines.length, addedLines.length);

      for (let j = 0; j < maxLen; j++) {
        left.push(
          j < removedLines.length
            ? { content: removedLines[j], type: "removed", lineNumber: leftNum++ }
            : { content: "", type: "empty", lineNumber: null }
        );
        right.push(
          j < addedLines.length
            ? { content: addedLines[j], type: "added", lineNumber: rightNum++ }
            : { content: "", type: "empty", lineNumber: null }
        );
      }

      i += nextChange?.added ? 2 : 1;
    } else {
      for (const line of splitLines(change.value)) {
        left.push({ content: "", type: "empty", lineNumber: null });
        right.push({ content: line, type: "added", lineNumber: rightNum++ });
      }
      i++;
    }
  }

  return { left, right };
}

function buildUnified(oldText: string, newText: string): UnifiedLine[] {
  const changes = diffLines(oldText, newText);
  const lines: UnifiedLine[] = [];
  let leftNum = 1;
  let rightNum = 1;

  for (const change of changes) {
    const parts = splitLines(change.value);
    if (!change.added && !change.removed) {
      for (const line of parts) {
        lines.push({ content: line, type: "same", leftNum: leftNum++, rightNum: rightNum++ });
      }
    } else if (change.removed) {
      for (const line of parts) {
        lines.push({ content: line, type: "removed", leftNum: leftNum++, rightNum: null });
      }
    } else {
      for (const line of parts) {
        lines.push({ content: line, type: "added", leftNum: null, rightNum: rightNum++ });
      }
    }
  }
  return lines;
}

// ─── Style maps ──────────────────────────────────────────────────────────────

const rowBg: Record<DiffLine["type"], string> = {
  same: "",
  removed: "bg-red-500/[0.07] dark:bg-red-500/[0.12]",
  added: "bg-emerald-500/[0.07] dark:bg-emerald-500/[0.12]",
  empty: "bg-muted/20 dark:bg-muted/10",
};

const gutterCls: Record<DiffLine["type"], string> = {
  same: "text-muted-foreground/30",
  removed: "bg-red-500/[0.08] dark:bg-red-500/[0.15] text-red-500/55",
  added: "bg-emerald-500/[0.08] dark:bg-emerald-500/[0.15] text-emerald-500/55",
  empty: "bg-muted/20 text-transparent",
};

const symbolCls: Record<DiffLine["type"], string> = {
  same: "text-transparent select-none",
  removed: "text-red-500/60 select-none",
  added: "text-emerald-500/60 select-none",
  empty: "text-transparent select-none",
};

const symbolChar: Record<DiffLine["type"], string> = {
  same: " ",
  removed: "−",
  added: "+",
  empty: " ",
};

// ─── Split line row ───────────────────────────────────────────────────────────
//
// KEY FIX: Each panel is its own flex column with overflow-auto.
// The <pre> uses whitespace-pre freely — the *panel* scrolls, not the row.
// This prevents the old grid-cols-2 + whitespace-pre overlap bug.

function SplitLine({ line }: { line: DiffLine }) {
  return (
    <div className={cn("flex items-stretch", rowBg[line.type])} style={{ height: 21 }}>
      <div className={cn(
        "shrink-0 w-5 flex items-center justify-center text-[9px] font-bold font-mono",
        symbolCls[line.type]
      )}>
        {symbolChar[line.type]}
      </div>
      <div className={cn(
        "shrink-0 w-8 flex items-center justify-end pr-1.5 text-[10px] tabular-nums font-mono border-r border-border/20",
        gutterCls[line.type]
      )}>
        {line.lineNumber ?? ""}
      </div>
      <pre className="px-3 text-[12px] leading-[21px] whitespace-pre font-mono text-foreground">
        {line.content || " "}
      </pre>
    </div>
  );
}

// ─── Unified line row ─────────────────────────────────────────────────────────

function UnifiedDiffLine({ line }: { line: UnifiedLine }) {
  const bg =
    line.type === "removed" ? "bg-red-500/[0.07] dark:bg-red-500/[0.12]"
    : line.type === "added" ? "bg-emerald-500/[0.07] dark:bg-emerald-500/[0.12]"
    : "";

  const numCls =
    line.type === "removed"
      ? "bg-red-500/[0.08] dark:bg-red-500/[0.15] text-red-500/55"
      : line.type === "added"
      ? "bg-emerald-500/[0.08] dark:bg-emerald-500/[0.15] text-emerald-500/55"
      : "text-muted-foreground/30";

  const sym = line.type === "removed" ? "−" : line.type === "added" ? "+" : " ";
  const symCls =
    line.type === "removed" ? "text-red-500/60"
    : line.type === "added" ? "text-emerald-500/60"
    : "text-transparent";

  return (
    <div className={cn("flex items-stretch", bg)} style={{ height: 21 }}>
      <div className={cn(
        "shrink-0 w-8 flex items-center justify-end pr-1.5 text-[10px] tabular-nums font-mono border-r border-border/20",
        numCls
      )}>
        {line.leftNum ?? ""}
      </div>
      <div className={cn(
        "shrink-0 w-8 flex items-center justify-end pr-1.5 text-[10px] tabular-nums font-mono border-r border-border/20",
        numCls
      )}>
        {line.rightNum ?? ""}
      </div>
      <div className={cn(
        "shrink-0 w-5 flex items-center justify-center text-[9px] font-bold font-mono select-none",
        symCls
      )}>
        {sym}
      </div>
      <pre className="px-3 text-[12px] leading-[21px] whitespace-pre font-mono text-foreground">
        {line.content || " "}
      </pre>
    </div>
  );
}

// ─── Main component ───────────────────────────────────────────────────────────

interface DiffViewerProps {
  oldText: string;
  newText: string;
  oldLabel: string;
  newLabel: string;
}

export function DiffViewer({ oldText, newText, oldLabel, newLabel }: DiffViewerProps) {
  const [viewMode, setViewMode] = useState<ViewMode>("split");

  const sideBySide = useMemo(() => buildSideBySide(oldText, newText), [oldText, newText]);
  const unified = useMemo(() => buildUnified(oldText, newText), [oldText, newText]);

  // Synchronized vertical scroll for split view
  const leftRef = useRef<HTMLDivElement>(null);
  const rightRef = useRef<HTMLDivElement>(null);
  const isSyncing = useRef(false);

  const handleLeftScroll = () => {
    if (isSyncing.current || !rightRef.current || !leftRef.current) return;
    isSyncing.current = true;
    rightRef.current.scrollTop = leftRef.current.scrollTop;
    isSyncing.current = false;
  };

  const handleRightScroll = () => {
    if (isSyncing.current || !leftRef.current || !rightRef.current) return;
    isSyncing.current = true;
    leftRef.current.scrollTop = rightRef.current.scrollTop;
    isSyncing.current = false;
  };

  const addedCount = sideBySide.right.filter((l) => l.type === "added").length;
  const removedCount = sideBySide.left.filter((l) => l.type === "removed").length;

  if (addedCount === 0 && removedCount === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-1.5 text-muted-foreground">
        <p className="text-sm font-medium">Conteúdos idênticos</p>
        <p className="text-xs opacity-60">Nenhuma diferença encontrada entre as versões.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Toolbar */}
      <div className="flex items-center justify-between px-3 py-1.5 border-b border-border/50 bg-muted/5 shrink-0">
        <div className="flex items-center gap-3">
          <span className="text-[10.5px] font-mono font-medium text-emerald-600 dark:text-emerald-400">
            +{addedCount}
          </span>
          <span className="text-[10.5px] font-mono font-medium text-red-600 dark:text-red-400">
            −{removedCount}
          </span>
          <span className="text-[10px] text-muted-foreground/40">linhas modificadas</span>
        </div>

        {/* View toggle */}
        <div className="flex items-center overflow-hidden rounded border border-border/50">
          <button
            onClick={() => setViewMode("split")}
            className={cn(
              "flex items-center gap-1 px-2 py-[3px] text-[10px] transition-colors",
              viewMode === "split"
                ? "bg-muted text-foreground font-medium"
                : "text-muted-foreground hover:bg-muted/40"
            )}
          >
            <Columns2 className="h-2.5 w-2.5" />
            Dividido
          </button>
          <div className="w-px h-3.5 bg-border/50" />
          <button
            onClick={() => setViewMode("unified")}
            className={cn(
              "flex items-center gap-1 px-2 py-[3px] text-[10px] transition-colors",
              viewMode === "unified"
                ? "bg-muted text-foreground font-medium"
                : "text-muted-foreground hover:bg-muted/40"
            )}
          >
            <AlignJustify className="h-2.5 w-2.5" />
            Unificado
          </button>
        </div>
      </div>

      {viewMode === "split" ? (
        // ── Split view ──
        // Two independent flex columns, each with overflow-auto.
        // Vertical scroll is synced via refs; horizontal scroll is independent per panel.
        // This is the structural fix for the overlap bug.
        <div className="flex flex-1 min-h-0 overflow-hidden">
          {/* Left / Old */}
          <div className="flex flex-col flex-1 min-w-0 border-r border-border/30 overflow-hidden">
            <div className="flex items-center gap-2 px-3 py-2 border-b border-border/40 bg-red-500/[0.03] shrink-0">
              <span className="size-1.5 rounded-full bg-red-400 shrink-0" />
              <span className="text-[11px] font-medium text-red-600 dark:text-red-400 truncate">
                {oldLabel}
              </span>
            </div>
            <div
              ref={leftRef}
              onScroll={handleLeftScroll}
              className="flex-1 overflow-auto"
            >
              {sideBySide.left.map((line, i) => (
                <SplitLine key={i} line={line} />
              ))}
            </div>
          </div>

          {/* Right / New */}
          <div className="flex flex-col flex-1 min-w-0 overflow-hidden">
            <div className="flex items-center gap-2 px-3 py-2 border-b border-border/40 bg-emerald-500/[0.03] shrink-0">
              <span className="size-1.5 rounded-full bg-emerald-400 shrink-0" />
              <span className="text-[11px] font-medium text-emerald-600 dark:text-emerald-400 truncate">
                {newLabel}
              </span>
            </div>
            <div
              ref={rightRef}
              onScroll={handleRightScroll}
              className="flex-1 overflow-auto"
            >
              {sideBySide.right.map((line, i) => (
                <SplitLine key={i} line={line} />
              ))}
            </div>
          </div>
        </div>
      ) : (
        // ── Unified view ──
        <div className="flex flex-col flex-1 min-h-0 overflow-hidden">
          <div className="grid grid-cols-2 border-b border-border/40 shrink-0">
            <div className="flex items-center gap-2 px-3 py-2 bg-red-500/[0.03] border-r border-border/30">
              <span className="size-1.5 rounded-full bg-red-400 shrink-0" />
              <span className="text-[11px] font-medium text-red-600 dark:text-red-400 truncate">
                {oldLabel}
              </span>
            </div>
            <div className="flex items-center gap-2 px-3 py-2 bg-emerald-500/[0.03]">
              <span className="size-1.5 rounded-full bg-emerald-400 shrink-0" />
              <span className="text-[11px] font-medium text-emerald-600 dark:text-emerald-400 truncate">
                {newLabel}
              </span>
            </div>
          </div>
          <div className="flex-1 overflow-auto">
            {unified.map((line, i) => (
              <UnifiedDiffLine key={i} line={line} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
