import { Outlet } from 'react-router-dom';
import Header from './Header';
import Footer from './Footer';

/**
 * Main Layout Component
 * 
 * Architecture:
 * - 3-part layout: Header (fixed) / Body (scrollable) / Footer (fixed)
 * - ONLY the body scrolls (single scroll design)
 * - Header and Footer are fixed height (no whole window scroll)
 * - Uses h-screen for full viewport height
 */
const Layout: React.FC = () => {
  return (
    <div className="h-screen flex flex-col bg-slate-950">
      {/* Header - Fixed height, no scroll */}
      <Header />
      
      {/* Body - Flexible height, scrollable */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
      
      {/* Footer - Fixed height, no scroll */}
      <Footer />
    </div>
  );
};

export default Layout;

