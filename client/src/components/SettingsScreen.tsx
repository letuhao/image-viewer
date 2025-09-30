import React, { useState } from 'react';
import {
  Dialog,
  Transition,
} from '@headlessui/react';
import {
  CogIcon,
  ComputerDesktopIcon,
  PhotoIcon,
  UserIcon,
  ShieldCheckIcon,
  XMarkIcon,
  ChevronRightIcon,
} from '@heroicons/react/24/outline';
import CacheSettingsSection from './CacheSettingsSection';

interface SettingsSection {
  id: string;
  name: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  component: React.ComponentType<{ isOpen: boolean; onClose: () => void }>;
}

interface SettingsScreenProps {
  isOpen: boolean;
  onClose: () => void;
}

const SettingsScreen: React.FC<SettingsScreenProps> = ({ isOpen, onClose }) => {
  const [activeSection, setActiveSection] = useState<string | null>(null);

  const settingsSections: SettingsSection[] = [
    {
      id: 'cache',
      name: 'Cache Settings',
      description: 'Manage cache folders and storage distribution',
      icon: ComputerDesktopIcon,
      component: CacheSettingsSection,
    },
    // Future settings sections can be added here
    // {
    //   id: 'appearance',
    //   name: 'Appearance',
    //   description: 'Customize theme, colors, and display options',
    //   icon: PhotoIcon,
    //   component: AppearanceSettings,
    // },
    // {
    //   id: 'user',
    //   name: 'User Settings',
    //   description: 'Manage user preferences and account settings',
    //   icon: UserIcon,
    //   component: UserSettings,
    // },
    // {
    //   id: 'security',
    //   name: 'Security',
    //   description: 'Privacy and security preferences',
    //   icon: ShieldCheckIcon,
    //   component: SecuritySettings,
    // },
  ];

  const handleSectionClick = (sectionId: string) => {
    setActiveSection(sectionId);
  };

  const handleBackToMain = () => {
    setActiveSection(null);
  };

  const renderActiveSection = () => {
    const section = settingsSections.find(s => s.id === activeSection);
    if (!section) return null;

    const SectionComponent = section.component;
    return (
      <SectionComponent
        isOpen={true}
        onClose={handleBackToMain}
      />
    );
  };

  return (
    <Transition appear show={isOpen} as={React.Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={React.Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-25" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4 text-center">
            <Transition.Child
              as={React.Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-4xl transform overflow-hidden rounded-2xl bg-white dark:bg-dark-800 p-6 text-left align-middle shadow-xl transition-all">
                {/* Header */}
                <div className="flex items-center justify-between mb-6">
                  <div className="flex items-center">
                    <CogIcon className="h-6 w-6 mr-2 text-gray-600 dark:text-gray-400" />
                    <Dialog.Title as="h3" className="text-lg font-medium leading-6 text-gray-900 dark:text-white">
                      Settings
                    </Dialog.Title>
                  </div>
                  <button
                    onClick={onClose}
                    className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 focus:outline-none"
                  >
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>

                {/* Content */}
                {!activeSection ? (
                  /* Main Settings Menu */
                  <div className="space-y-4">
                    <div className="text-sm text-gray-600 dark:text-gray-400 mb-6">
                      Configure your image viewer preferences and settings.
                    </div>
                    
                    <div className="grid gap-4">
                      {settingsSections.map((section) => (
                        <button
                          key={section.id}
                          onClick={() => handleSectionClick(section.id)}
                          className="flex items-center p-4 bg-gray-50 dark:bg-dark-700 rounded-lg hover:bg-gray-100 dark:hover:bg-dark-600 transition-colors text-left w-full"
                        >
                          <div className="flex-shrink-0">
                            <section.icon className="h-8 w-8 text-gray-600 dark:text-gray-400" />
                          </div>
                          <div className="ml-4 flex-1">
                            <h3 className="text-lg font-medium text-gray-900 dark:text-white">
                              {section.name}
                            </h3>
                            <p className="text-sm text-gray-600 dark:text-gray-400">
                              {section.description}
                            </p>
                          </div>
                          <div className="flex-shrink-0">
                            <ChevronRightIcon className="h-5 w-5 text-gray-400" />
                          </div>
                        </button>
                      ))}
                    </div>

                    {/* Coming Soon Section */}
                    <div className="mt-8 pt-6 border-t border-gray-200 dark:border-gray-600">
                      <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-4">
                        Coming Soon
                      </h4>
                      <div className="grid gap-4">
                        <div className="flex items-center p-4 bg-gray-100 dark:bg-dark-600 rounded-lg opacity-60">
                          <div className="flex-shrink-0">
                            <PhotoIcon className="h-8 w-8 text-gray-400" />
                          </div>
                          <div className="ml-4 flex-1">
                            <h3 className="text-lg font-medium text-gray-500 dark:text-gray-400">
                              Appearance
                            </h3>
                            <p className="text-sm text-gray-400 dark:text-gray-500">
                              Customize theme, colors, and display options
                            </p>
                          </div>
                          <div className="flex-shrink-0">
                            <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400 px-2 py-1 rounded">
                              Soon
                            </span>
                          </div>
                        </div>

                        <div className="flex items-center p-4 bg-gray-100 dark:bg-dark-600 rounded-lg opacity-60">
                          <div className="flex-shrink-0">
                            <UserIcon className="h-8 w-8 text-gray-400" />
                          </div>
                          <div className="ml-4 flex-1">
                            <h3 className="text-lg font-medium text-gray-500 dark:text-gray-400">
                              User Settings
                            </h3>
                            <p className="text-sm text-gray-400 dark:text-gray-500">
                              Manage user preferences and account settings
                            </p>
                          </div>
                          <div className="flex-shrink-0">
                            <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400 px-2 py-1 rounded">
                              Soon
                            </span>
                          </div>
                        </div>

                        <div className="flex items-center p-4 bg-gray-100 dark:bg-dark-600 rounded-lg opacity-60">
                          <div className="flex-shrink-0">
                            <ShieldCheckIcon className="h-8 w-8 text-gray-400" />
                          </div>
                          <div className="ml-4 flex-1">
                            <h3 className="text-lg font-medium text-gray-500 dark:text-gray-400">
                              Security
                            </h3>
                            <p className="text-sm text-gray-400 dark:text-gray-500">
                              Privacy and security preferences
                            </p>
                          </div>
                          <div className="flex-shrink-0">
                            <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400 px-2 py-1 rounded">
                              Soon
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  /* Active Section */
                  <div>
                    {/* Section Header with Back Button */}
                    <div className="flex items-center mb-6">
                      <button
                        onClick={handleBackToMain}
                        className="mr-4 p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 focus:outline-none"
                      >
                        <ChevronRightIcon className="h-5 w-5 rotate-180" />
                      </button>
                      <div className="flex items-center">
                        {(() => {
                          const section = settingsSections.find(s => s.id === activeSection);
                          return section ? (
                            <>
                              <section.icon className="h-6 w-6 mr-2 text-gray-600 dark:text-gray-400" />
                              <h2 className="text-lg font-medium text-gray-900 dark:text-white">
                                {section.name}
                              </h2>
                            </>
                          ) : null;
                        })()}
                      </div>
                    </div>

                    {/* Section Content */}
                    <div className="max-h-96 overflow-y-auto">
                      {renderActiveSection()}
                    </div>
                  </div>
                )}
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};

export default SettingsScreen;
