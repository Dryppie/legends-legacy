export interface SidebarSection {
  id: string;
  label: string;
  items: Tab[];
}

export interface Tab {
  id: string;
  route: string[];
  icon: string;
  activeIcon?: string;
  title: string;
  description?: string;
}
