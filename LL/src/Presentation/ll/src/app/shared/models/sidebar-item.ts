export interface Tab {
  label: string; // Label of the tab (e.g., Daily, Weekly, Gathering, etc.)
  items: any[]; // List of items for this tab
}

export interface SidebarItem {
  id: string;
  route: string[];
  icon: string;
  title: string;
  description: string;
  rewards?: { icon: string; amount: number }[];
}
