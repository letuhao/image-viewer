import React from 'react';
import { GlobeAltIcon } from '@heroicons/react/24/outline';

interface LanguageSelectorProps {
  language: string;
  onLanguageChange: (language: string) => void;
  availableLanguages?: string[];
  className?: string;
}

const LanguageSelector: React.FC<LanguageSelectorProps> = ({
  language,
  onLanguageChange,
  availableLanguages = ['en', 'zh', 'ko', 'ja'],
  className = ''
}) => {
  const getLanguageName = (lang: string) => {
    const names: Record<string, string> = {
      'en': 'English',
      'zh': '中文',
      'ko': '한국어',
      'ja': '日本語'
    };
    return names[lang] || lang.toUpperCase();
  };

  const getLanguageFlag = (lang: string) => {
    const flags: Record<string, string> = {
      'en': '🇺🇸',
      'zh': '🇨🇳',
      'ko': '🇰🇷',
      'ja': '🇯🇵'
    };
    return flags[lang] || '🌐';
  };

  return (
    <div className={`flex items-center space-x-2 ${className}`}>
      <GlobeAltIcon className="h-4 w-4 text-gray-500" />
      <select
        value={language}
        onChange={(e) => onLanguageChange(e.target.value)}
        className="text-sm border border-gray-300 dark:border-gray-600 rounded px-2 py-1 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
      >
        {availableLanguages.map(lang => (
          <option key={lang} value={lang}>
            {getLanguageFlag(lang)} {getLanguageName(lang)}
          </option>
        ))}
      </select>
    </div>
  );
};

export default LanguageSelector;
