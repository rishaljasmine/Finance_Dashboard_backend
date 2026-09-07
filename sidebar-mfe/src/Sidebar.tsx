import { useEffect, useState } from 'react';
import { CHART_TYPES, type SidebarProps } from './types';

const MOBILE_BREAKPOINT = '(max-width: 900px)';

function isMobileViewport(): boolean {
  return window.matchMedia(MOBILE_BREAKPOINT).matches;
}

// A straight port of the Angular sidebar's <aside> + mobile backdrop.
// Everything about *how it looks and moves* (collapse, mobile drawer,
// the Financial Overview submenu) is decided right here, same as before.
// The only thing that changed is *which section is active* — that used to
// just be a local field this component could flip directly; now it's owned
// by whatever page embeds this sidebar, and this component only asks for
// it to change via onNavigate/onChartSelect.
export function Sidebar({
  activeSection,
  selectedChart,
  onNavigate,
  onChartSelect,
  onLayoutChange,
}: SidebarProps) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [overviewMenuOpen, setOverviewMenuOpen] = useState(false);

  // Report outward whenever the sidebar's own width would change, so the
  // host can keep its main-content margin in sync without needing to know
  // *why* the width changed — just that it did.
  useEffect(() => {
    onLayoutChange?.({ collapsed: sidebarCollapsed, mobileOpen: mobileSidebarOpen });
  }, [sidebarCollapsed, mobileSidebarOpen, onLayoutChange]);

  function toggleSidebar(): void {
    if (isMobileViewport()) {
      setMobileSidebarOpen((open) => !open);
      return;
    }
    setSidebarCollapsed((collapsed) => !collapsed);
  }

  function closeMobileSidebar(): void {
    setMobileSidebarOpen(false);
  }

  function selectSection(section: Parameters<typeof onNavigate>[0]): void {
    onNavigate(section);
    if (isMobileViewport()) {
      setMobileSidebarOpen(false);
    }
  }

  function onDashboardIconClick(): void {
    if (isMobileViewport()) {
      // On mobile, the collapsed rail shows only this icon, so it
      // doubles as the menu opener when the drawer is closed.
      if (!mobileSidebarOpen) {
        setMobileSidebarOpen(true);
        return;
      }
      selectSection('home');
      return;
    }

    // Desktop: mirror the same open/close toggle — a collapsed rail
    // expands, an expanded sidebar navigates home and collapses back.
    if (sidebarCollapsed) {
      setSidebarCollapsed(false);
      return;
    }

    onNavigate('home');
    setSidebarCollapsed(true);
  }

  function toggleOverviewMenu(event: React.MouseEvent): void {
    event.stopPropagation();
    setOverviewMenuOpen((open) => !open);
  }

  function selectOverviewSection(): void {
    // Deliberately not calling selectSection() here: on mobile that
    // would auto-close the drawer, but the intent when tapping this row
    // is to browse the chart submenu it's about to reveal — same as
    // tapping the chevron directly, which stays open.
    onNavigate('overview');
    setOverviewMenuOpen(true);
  }

  function selectChartFromSidebar(type: Parameters<typeof onChartSelect>[0]): void {
    onChartSelect(type);
    selectSection('overview');
  }

  const navItemClass = (active: boolean) =>
    active
      ? 'text-[#f8fafc] bg-[#171b2a] light:text-[#111827] light:bg-[#eef2ff] light:border light:border-[#4338ca]'
      : 'text-[#8390a3] bg-transparent light:text-[#6b7280]';

  return (
    <>
      {/* SIDEBAR */}
      <aside
        className={[
          'fixed left-0 top-0 bottom-0 flex flex-col border-r border-[#1b2635] light:border-[#e5e7eb] z-[1000]',
          '[background:linear-gradient(180deg,#090f19,#080d15)] light:[background:#fafbfe]',
          'transition-[width,transform] duration-[280ms] ease-in-out',
          sidebarCollapsed
            ? 'min-[901px]:w-[82px] min-[901px]:pl-[10px] min-[901px]:pr-[10px] min-[901px]:py-[28px]'
            : 'min-[901px]:w-[245px] min-[901px]:px-[20px] min-[901px]:py-[28px]',
          mobileSidebarOpen
            ? 'max-[900px]:w-[260px] max-[900px]:pl-[18px] max-[900px]:pr-[18px] max-[900px]:py-[22px] max-[900px]:shadow-[15px_0_45px_rgba(0,0,0,.45)]'
            : 'max-[900px]:w-[82px] max-[900px]:px-[10px] max-[900px]:py-[22px]',
          'max-[900px]:translate-x-0',
        ].join(' ')}
      >
        <div className="w-full h-full flex flex-col transition-all duration-[250ms]">
          {/* LOGO */}
          <div
            className={
              'flex items-center gap-[12px] mb-[42px] px-[4px] py-0' +
              (sidebarCollapsed ? ' justify-center px-0' : '')
            }
          >
            <button
              type="button"
              className="w-[38px] h-[38px] grid place-items-center shrink-0 text-[#8b5cf6] bg-[#17152b] border border-[#38305e] rounded-[11px] cursor-pointer light:text-[#7c3aed] light:bg-[#f3f0ff] light:border-[#ddd6fe]"
              style={{ font: 'inherit' }}
              onClick={toggleSidebar}
              aria-label="Toggle sidebar"
            >
              <svg className="w-[23px] h-[23px]" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M4 7.5A2.5 2.5 0 0 1 6.5 5H19a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H6.5A2.5 2.5 0 0 1 4 16.5v-9Z"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                />
                <path
                  d="M4 8h14.5A2.5 2.5 0 0 1 21 10.5V13H17a2.5 2.5 0 0 1 0-5h3.5"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <circle cx="17" cy="10.5" r="0.9" fill="currentColor" />
              </svg>
            </button>

            <div
              className={
                (sidebarCollapsed ? 'hidden' : '') +
                ' ' +
                (!mobileSidebarOpen ? 'max-[900px]:hidden' : '')
              }
            >
              <h2 className="text-[#f8fafc] text-[19px] leading-[1.1] block m-0 light:text-[#111827]">
                Finova
              </h2>
              <span className="block m-0 mt-[4px] text-[#7f8ba0] text-[10px] light:text-[#6b7280]">
                Finance Dashboard
              </span>
            </div>
          </div>

          {/* DASHBOARD LINK */}
          <a
            className={
              'relative group flex items-center gap-[12px] w-full min-h-[44px] px-[15px] py-0 border-0 rounded-[9px] no-underline cursor-pointer mb-[8px] shrink-0 ' +
              (activeSection === 'home'
                ? 'text-[#f8fafc] bg-[#171b2a] shadow-[inset_3px_0_#8b5cf6] light:text-[#111827] light:bg-[#eef2ff] light:border light:border-[#4338ca]'
                : 'text-[#8f9bad] bg-transparent light:text-[#64748b]') +
              (sidebarCollapsed ? ' justify-center pl-0 pr-0' : '')
            }
            style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
            href="/dashboard"
            onClick={(event) => {
              event.preventDefault();
              onDashboardIconClick();
            }}
          >
            <span className="w-[18px] shrink-0 grid place-items-center text-[#a78bfa] light:text-[#7c3aed]">
              <svg className="w-[18px] h-[18px]" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M3.5 11.5 12 4l8.5 7.5"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M5.5 10v8.5a1 1 0 0 0 1 1h11a1 1 0 0 0 1-1V10"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>

            <span
              className={
                (sidebarCollapsed ? 'hidden' : '') +
                ' ' +
                (!mobileSidebarOpen ? 'max-[900px]:hidden' : '')
              }
            >
              Dashboard
            </span>

            {sidebarCollapsed && (
              <span className="pointer-events-none absolute left-full ml-[12px] top-1/2 -translate-y-1/2 whitespace-nowrap px-[10px] py-[6px] rounded-[7px] text-[12px] font-semibold text-white bg-[#1c2432] border border-[#2a3648] shadow-[0_8px_20px_rgba(0,0,0,.35)] opacity-0 scale-95 transition-[opacity,transform] duration-150 z-[1100] group-hover:opacity-100 group-hover:scale-100 light:bg-[#111827] light:border-[#1f2937]">
                Dashboard
              </span>
            )}
          </a>

          {/* NAVIGATION */}
          <nav
            className="grid content-start gap-[8px] flex-1 min-h-0 overflow-y-auto overflow-x-hidden"
            style={{ WebkitOverflowScrolling: 'touch', scrollbarWidth: 'thin' }}
          >
            <div
              className={
                'grid content-start gap-[4px] pl-[10px] ' +
                (sidebarCollapsed ? 'hidden' : '') +
                ' ' +
                (!mobileSidebarOpen ? 'max-[900px]:hidden' : '')
              }
            >
              {/* FINANCIAL OVERVIEW (expandable) */}
              <button
                type="button"
                className={
                  'flex items-center justify-between w-full min-h-[38px] px-[12px] py-0 border-0 rounded-[8px] cursor-pointer text-[13px] ' +
                  navItemClass(activeSection === 'overview')
                }
                style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
                onClick={selectOverviewSection}
              >
                <span className="flex items-center gap-[10px]">
                  <span className="text-[15px]">▥</span>
                  Financial Overview
                </span>

                <span
                  className={
                    'text-[10px] transition-transform duration-200' +
                    (overviewMenuOpen ? ' [transform:rotate(90deg)]' : '')
                  }
                  onClick={toggleOverviewMenu}
                >
                  ▸
                </span>
              </button>

              {overviewMenuOpen && (
                <div className="grid gap-[2px] pl-[20px] overflow-hidden">
                  {CHART_TYPES.map((chart) => (
                    <a
                      key={chart.type}
                      className={
                        'flex items-center gap-[8px] min-h-[32px] px-[10px] py-0 rounded-[7px] cursor-pointer text-[12px] no-underline ' +
                        (activeSection === 'overview' && selectedChart === chart.type
                          ? 'text-[#c4b5fd] bg-[#1c1740] light:text-[#4338ca] light:bg-[#eef2ff] light:border light:border-[#4338ca]'
                          : 'text-[#75808f] bg-transparent light:text-[#94a3b8]')
                      }
                      style={{ transition: 'background 0.2s, color 0.2s' }}
                      onClick={() => selectChartFromSidebar(chart.type)}
                    >
                      <span>{chart.icon}</span>
                      {chart.name}
                    </a>
                  ))}
                </div>
              )}

              {/* RECENT TRANSACTIONS */}
              <a
                className={
                  'flex items-center gap-[10px] w-full min-h-[38px] px-[12px] py-0 rounded-[8px] cursor-pointer text-[13px] no-underline ' +
                  navItemClass(activeSection === 'transactions')
                }
                style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
                onClick={() => selectSection('transactions')}
              >
                <span className="text-[15px]">↗</span>
                Recent Transactions
              </a>

              {/* EXPENSES BY CATEGORY */}
              <a
                className={
                  'flex items-center gap-[10px] w-full min-h-[38px] px-[12px] py-0 rounded-[8px] cursor-pointer text-[13px] no-underline ' +
                  navItemClass(activeSection === 'expenses')
                }
                style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
                onClick={() => selectSection('expenses')}
              >
                <span className="text-[15px]">◉</span>
                Expenses by Category
              </a>

              {/* SUMMARY */}
              <a
                className={
                  'flex items-center gap-[10px] w-full min-h-[38px] px-[12px] py-0 rounded-[8px] cursor-pointer text-[13px] no-underline ' +
                  navItemClass(activeSection === 'summary')
                }
                style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
                onClick={() => selectSection('summary')}
              >
                <span className="text-[15px]">☰</span>
                Summary
              </a>

              {/* FILES */}
              <a
                className={
                  'flex items-center gap-[10px] w-full min-h-[38px] px-[12px] py-0 rounded-[8px] cursor-pointer text-[13px] no-underline ' +
                  navItemClass(activeSection === 'files')
                }
                style={{ font: 'inherit', transition: 'background 0.2s, color 0.2s' }}
                onClick={() => selectSection('files')}
              >
                <span className="w-[15px] shrink-0 grid place-items-center">
                  <svg className="w-[15px] h-[15px]" viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      d="M6.5 3.5h7l5 5v11a1 1 0 0 1-1 1h-11a1 1 0 0 1-1-1v-15a1 1 0 0 1 1-1Z"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.7"
                      strokeLinejoin="round"
                    />
                    <path
                      d="M13.5 3.5v4a1 1 0 0 0 1 1h4"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.7"
                      strokeLinejoin="round"
                    />
                  </svg>
                </span>
                Files
              </a>
            </div>
          </nav>
        </div>
      </aside>

      {/* MOBILE BACKDROP */}
      {mobileSidebarOpen && (
        <div
          className="hidden max-[900px]:block fixed inset-0 z-[900]"
          style={{ background: 'rgba(0, 0, 0, 0.55)', backdropFilter: 'blur(2px)' }}
          onClick={closeMobileSidebar}
        />
      )}
    </>
  );
}
