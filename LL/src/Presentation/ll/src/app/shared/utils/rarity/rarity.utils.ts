import { Rarity } from "../../models/enums/rarity";

export function getRarityColor(rarity: Rarity): string {
  switch (rarity) {
    case Rarity.Common: return '#e2e8f0';       // slate-200
    case Rarity.Uncommon: return '#059669';     // emerald-600
    case Rarity.Rare: return '#2563eb';         // blue-600
    case Rarity.Epic: return '#a21caf';         // fuchsia-600
    case Rarity.Unique: return '#facc15';       // yellow-400
    case Rarity.Legendary: return '#ea580c';    // orange-600
    case Rarity.Legacy: return '#be123c';       // rose-700
    default: return '#cbd5e1';                  // light_gray fallback
  }
}
