


// ----- Enum mapping helpers-----
// EntryStatus: UI -> enum
export function uiStatusToEnum(v) {
    switch ((v || "").toLowerCase()) {
        case "watching":        return "Watching";
        case "completed":       return "Completed";
        case "on-hold" :        return "OnHold";
        case "dropped":         return "Dropped";
        case "plan-to-watch":   return "Planning";
        default:                return "Planning";
    }
}

// EntryStatus: enum -> UI
export function enumStatusToUI(s) {
    switch ((s || "").toLowerCase()) {
        case "watching":        return "watching";
        case "completed":       return "completed";
        case "onhold" :        return "on-hold";
        case "dropped":         return "dropped";
        case "planning":        return "plan-to-watch";
        default:                return "plan-to-watch";
    }
}

// EntryType: UI -> Enum
export function uiTypeToEnum(v) {
    const t = (v || "").toLowerCase();
    if (t === "movie") return "Movie";
    if (t === "series") return "Series";
    return undefined;
}

// EntryType: Enum -> UI
export function enumTypeToUI(v) {
    const t = (v || "").toLowerCase();
    if (t === "movie") return "movie";
    if (t === "series") return "series";
    return "movie";
}

// EntrySubType: UI -> Enum
export function uiSubTypeToEnum(v) {
    switch ((v || "").toLowerCase()) {
        case "anime":               return "Anime";
        case "live-action":         return "LiveAction";
        case "animated" :           return "Animated";
        case "documentary":         return "Documentary";
        case "manga":               return "Manga";
        default:                    return undefined;
    }
}

// EntrySubType: Enum -> UI
export function enumSubTypeToUI(v) {
    switch ((v || "").toLowerCase()) {
        case "anime":               return "anime";
        case "liveaction":          return "live-action";
        case "animated" :           return "animated";
        case "documentary":         return "documentary";
        case "manga":               return "manga";
        default:                    return "live-action";
    }
}

 // Convert a PascalCase enum string into a human label:
 // "LiveAction" -> "Live Action", "OnHold" -> "On Hold".
export function enumStringToLabel(v) {
  return String(v ?? "")
    .replace(/([a-z])([A-Z])/g, "$1 $2") // split camel/pascal case
    .trim();
}