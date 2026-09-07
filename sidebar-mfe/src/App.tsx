import { useState } from 'react';
import { Sidebar } from './Sidebar';
import type { DashboardChartType, DashboardSection } from './types';

// Stands in for the Angular host: owns activeSection/selectedChart (the
// "controlled" state) and the light/dark toggle, then hands them to the
// sidebar as plain props — exactly the contract a real host would use.
export default function App() {
  const [activeSection, setActiveSection] = useState<DashboardSection>('home');
  const [selectedChart, setSelectedChart] = useState<DashboardChartType>('bar');
  const [isLight, setIsLight] = useState(false);

  function toggleTheme(): void {
    const next = !isLight;
    setIsLight(next);
    document.documentElement.classList.toggle('light', next);
  }

  return (
    <div className="min-h-screen bg-[#080b12] light:bg-[#f4f6fa]">
      <Sidebar
        activeSection={activeSection}
        selectedChart={selectedChart}
        onNavigate={setActiveSection}
        onChartSelect={setSelectedChart}
        onLayoutChange={(state) => console.log('layout-change', state)}
      />

      {/* Fake "main content" area, just enough to prove the sidebar is
          correctly reporting clicks outward instead of handling them
          itself. */}
      <main className="ml-[245px] max-[900px]:ml-[82px] p-[32px] text-[#e5e7eb] light:text-[#1f2430]">
        <button
          type="button"
          onClick={toggleTheme}
          className="mb-[24px] px-[14px] py-[8px] rounded-[8px] border border-[#253044] bg-[#101722] text-[#aab4c4] cursor-pointer light:bg-white light:border-[#e5e7eb] light:text-[#374151]"
        >
          Toggle {isLight ? 'dark' : 'light'} mode
        </button>

        <h1 className="text-[22px] font-semibold mb-[8px]">Sidebar MFE — standalone preview</h1>
        <p className="text-[13px] text-[#7f8ba0] light:text-[#6b7280]">
          This page is standing in for the Angular host. Click around the sidebar —
          the values below are what the sidebar reported outward via props, not
          anything it decided on its own.
        </p>

        <dl className="mt-[20px] text-[13px] grid gap-[6px]">
          <div>
            <dt className="inline text-[#7f8ba0] light:text-[#6b7280]">activeSection:</dt>{' '}
            <dd className="inline font-semibold">{activeSection}</dd>
          </div>
          <div>
            <dt className="inline text-[#7f8ba0] light:text-[#6b7280]">selectedChart:</dt>{' '}
            <dd className="inline font-semibold">{selectedChart}</dd>
          </div>
        </dl>
      </main>
    </div>
  );
}
