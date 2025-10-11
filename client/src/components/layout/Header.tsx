import { Link, useLocation } from 'react-router-dom';
import { 
  Home, 
  FolderOpen, 
  Settings, 
  Activity,
  HardDrive,
  Menu,
  X,
  LogOut,
  User
} from 'lucide-react';
import { useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';

/**
 * Header Component
 * 
 * Design:
 * - Fixed height (h-14 = 56px) to prevent window scroll
 * - Icon-based navigation for compact design
 * - Mobile responsive with hamburger menu
 * - Shows current route with active indicator
 */
const Header: React.FC = () => {
  const location = useLocation();
  const { user, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const navItems = [
    { path: '/', icon: Home, label: 'Dashboard' },
    { path: '/collections', icon: FolderOpen, label: 'Collections' },
    { path: '/jobs', icon: Activity, label: 'Jobs' },
    { path: '/cache', icon: HardDrive, label: 'Cache' },
    { path: '/settings', icon: Settings, label: 'Settings' },
  ];

  const isActive = (path: string) => {
    if (path === '/') {
      return location.pathname === '/';
    }
    return location.pathname.startsWith(path);
  };

  return (
    <header className="h-14 border-b border-slate-800 bg-slate-900 flex-shrink-0">
      <div className="h-full px-4 flex items-center justify-between">
        {/* Logo / Brand */}
        <Link to="/" className="flex items-center space-x-2 hover:opacity-80 transition-opacity">
          <Activity className="h-6 w-6 text-blue-500" />
          <span className="font-semibold text-lg text-white hidden sm:inline">
            ImageViewer
          </span>
        </Link>

        {/* Desktop Navigation */}
        <nav className="hidden md:flex items-center space-x-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            const active = isActive(item.path);
            
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`
                  flex items-center space-x-2 px-3 py-2 rounded-lg transition-all
                  ${active 
                    ? 'bg-blue-600 text-white' 
                    : 'text-slate-400 hover:text-white hover:bg-slate-800'
                  }
                `}
              >
                <Icon className="h-5 w-5" />
                <span className="text-sm font-medium">{item.label}</span>
              </Link>
            );
          })}
          
          {/* User Menu */}
          <div className="ml-4 pl-4 border-l border-slate-700 flex items-center space-x-2">
            <div className="text-sm text-slate-400">
              <User className="h-4 w-4 inline mr-1" />
              {user?.username}
            </div>
            <button
              onClick={logout}
              className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
              title="Logout"
            >
              <LogOut className="h-5 w-5" />
            </button>
          </div>
        </nav>

        {/* Mobile Menu Button */}
        <button
          onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          className="md:hidden p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
        >
          {isMobileMenuOpen ? (
            <X className="h-5 w-5" />
          ) : (
            <Menu className="h-5 w-5" />
          )}
        </button>
      </div>

      {/* Mobile Navigation Dropdown */}
      {isMobileMenuOpen && (
        <div className="md:hidden absolute top-14 left-0 right-0 bg-slate-900 border-b border-slate-800 z-50">
          <nav className="px-4 py-2 space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active = isActive(item.path);
              
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className={`
                    flex items-center space-x-3 px-3 py-2 rounded-lg transition-all
                    ${active 
                      ? 'bg-blue-600 text-white' 
                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                    }
                  `}
                >
                  <Icon className="h-5 w-5" />
                  <span className="text-sm font-medium">{item.label}</span>
                </Link>
              );
            })}
          </nav>
        </div>
      )}
    </header>
  );
};

export default Header;

