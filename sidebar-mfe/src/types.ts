// Ported 1:1 from the Angular app's dashboard.ts — these are the section
// and chart identifiers the rest of the dashboard already understands, so
// the sidebar keeps speaking the same "language" no matter which framework
// renders it.

export type DashboardChartType = 'bar' | 'line' | 'pie' | '3dline';

export type DashboardSection =
  | 'home'
  | 'summary'
  | 'overview'
  | 'transactions'
  | 'expenses'
  | 'files';

export interface ChartTypeDef {
  type: DashboardChartType;
  name: string;
  icon: string;
}

export const CHART_TYPES: ChartTypeDef[] = [
  { type: 'bar', name: 'Bar Chart', icon: '▥' },
  { type: 'line', name: 'Line Chart', icon: '⌁' },
  { type: 'pie', name: 'Pie Chart', icon: '◉' },
  { type: '3dline', name: '3D Line', icon: '〽' },
];

// What the sidebar needs to be told from the outside (the "controlled"
// bucket from the migration plan).
export interface SidebarProps {
  activeSection: DashboardSection;
  selectedChart: DashboardChartType;
  onNavigate: (section: DashboardSection) => void;
  onChartSelect: (chart: DashboardChartType) => void;
  // The host's main content area shifts its margin based on whether the
  // sidebar is collapsed/the mobile drawer is open — that's the one other
  // piece of sidebar-internal state the host actually needs to mirror,
  // purely for layout math, not for anything it decides.
  onLayoutChange?: (state: { collapsed: boolean; mobileOpen: boolean }) => void;
}
