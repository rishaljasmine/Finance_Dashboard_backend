import { createRoot, type Root } from 'react-dom/client';
import { Sidebar } from './Sidebar';
import type { DashboardChartType, DashboardSection } from './types';
import './index.css';

const DEFAULT_SECTION: DashboardSection = 'home';
const DEFAULT_CHART: DashboardChartType = 'bar';

/**
 * <finova-sidebar active-section="transactions" selected-chart="bar">
 *
 * The custom-element "adapter" the migration plan talked about: it
 * translates between HTML-land (attributes, DOM events) and React-land
 * (props, callbacks), so Sidebar.tsx itself never has to know it's being
 * embedded in a non-React page.
 *
 * Deliberately NOT using Shadow DOM: the sidebar's `light:` Tailwind
 * variant depends on seeing a `.light` class on an ANCESTOR <html>
 * element (see index.css). Shadow DOM would wall that off. The trade-off
 * is no style encapsulation — acceptable here since this element is
 * built from the exact same Tailwind setup as the host, on purpose.
 */
class FinovaSidebarElement extends HTMLElement {
  static observedAttributes = ['active-section', 'selected-chart'];

  #root: Root | null = null;

  connectedCallback(): void {
    this.#root = createRoot(this);
    this.#render();
  }

  disconnectedCallback(): void {
    this.#root?.unmount();
    this.#root = null;
  }

  attributeChangedCallback(): void {
    this.#render();
  }

  #render(): void {
    if (!this.#root) return;

    const activeSection =
      (this.getAttribute('active-section') as DashboardSection | null) ?? DEFAULT_SECTION;
    const selectedChart =
      (this.getAttribute('selected-chart') as DashboardChartType | null) ?? DEFAULT_CHART;

    this.#root.render(
      <Sidebar
        activeSection={activeSection}
        selectedChart={selectedChart}
        onNavigate={(section) => {
          this.dispatchEvent(
            new CustomEvent<{ section: DashboardSection }>('navigate', {
              detail: { section },
              bubbles: true,
              composed: true,
            }),
          );
        }}
        onChartSelect={(chart) => {
          this.dispatchEvent(
            new CustomEvent<{ chart: DashboardChartType }>('chart-select', {
              detail: { chart },
              bubbles: true,
              composed: true,
            }),
          );
        }}
        onLayoutChange={(state) => {
          this.dispatchEvent(
            new CustomEvent<{ collapsed: boolean; mobileOpen: boolean }>('layout-change', {
              detail: state,
              bubbles: true,
              composed: true,
            }),
          );
        }}
      />,
    );
  }
}

customElements.define('finova-sidebar', FinovaSidebarElement);
