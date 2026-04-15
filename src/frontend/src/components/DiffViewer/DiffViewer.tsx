import { useMemo } from "react";
import { diffLines } from "diff";
import { cn } from "@/lib/utils";

interface DiffLine {
  content: string;
  type: "same" | "added" | "removed" | "empty";
  lineNumber: number | null;
}

interface SideBySide {
  left: DiffLine[];
  right: DiffLine[];
}

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

const rowBg: Record<DiffLine["type"], string> = {
  same: "",
  removed: "bg-red-500/[0.07] dark:bg-red-500/[0.12]",
  added: "bg-emerald-500/[0.07] dark:bg-emerald-500/[0.12]",
  empty: "bg-muted/25",
};

const gutterBg: Record<DiffLine["type"], string> = {
  same: "text-muted-foreground/30",
  removed: "bg-red-500/[0.10] dark:bg-red-500/[0.18] text-red-500/60",
  added: "bg-emerald-500/[0.10] dark:bg-emerald-500/[0.18] text-emerald-500/60",
  empty: "bg-muted/30 text-muted-foreground/20",
};

const gutterSymbol: Record<DiffLine["type"], string> = {
  same: "",
  removed: "−",
  added: "+",
  empty: "",
};

function DiffCell({ line, className }: { line: DiffLine; className?: string }) {
  return (
    <div className={cn("flex min-w-0", rowBg[line.type], className)}>
      <div
        className={cn(
          "shrink-0 w-11 flex items-center justify-between px-1.5 text-[10px] tabular-nums select-none border-r border-border/30",
          gutterBg[line.type]
        )}
      >
        <span className="font-semibold leading-none">{gutterSymbol[line.type]}</span>
        <span className="leading-none">{line.lineNumber ?? ""}</span>
      </div>
      <pre className="flex-1 px-3 py-[3px] text-[11.5px] leading-[1.6] whitespace-pre font-mono tracking-tight">
        {line.content || " "}
      </pre>
    </div>
  );
}

interface DiffViewerProps {
  oldText: string;
  newText: string;
  oldLabel: string;
  newLabel: string;
}

export function DiffViewer({ oldText, newText, oldLabel, newLabel }: DiffViewerProps) {
  const { left, right } = useMemo(
    () => buildSideBySide(oldText, newText),
    [oldText, newText]
  );

  if (left.length === 0 && right.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-muted-foreground italic">
        Conteúdos idênticos — nenhuma diferença encontrada.
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto text-foreground">
      {/* Sticky column headers */}
      <div className="sticky top-0 z-10 grid grid-cols-2 border-b bg-background/95 backdrop-blur-sm">
        <div className="flex items-center gap-2 px-3 py-2 border-r border-border/50 bg-red-500/[0.04] dark:bg-red-500/[0.07]">
          <span className="size-1.5 rounded-full bg-red-400 shrink-0" />
          <span className="text-[11px] font-medium text-red-700 dark:text-red-400 truncate">
            {oldLabel}
          </span>
        </div>
        <div className="flex items-center gap-2 px-3 py-2 bg-emerald-500/[0.04] dark:bg-emerald-500/[0.07]">
          <span className="size-1.5 rounded-full bg-emerald-400 shrink-0" />
          <span className="text-[11px] font-medium text-emerald-700 dark:text-emerald-400 truncate">
            {newLabel}
          </span>
        </div>
      </div>

      {/* Diff rows */}
      {left.map((leftLine, i) => (
        <div
          key={i}
          className="grid grid-cols-2 border-b border-border/15 last:border-0"
        >
          <DiffCell line={leftLine} className="border-r border-border/25" />
          <DiffCell line={right[i]} />
        </div>
      ))}
    </div>
  );
}
