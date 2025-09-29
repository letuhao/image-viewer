import React, { useState, useEffect, useRef } from 'react';
import { 
  XMarkIcon, 
  GlobeAltIcon,
  SparklesIcon
} from '@heroicons/react/24/outline';
import { statsApi } from '../services/api';

interface EnhancedTagAutocompleteProps {
  selectedTags: string[];
  onTagsChange: (tags: string[]) => void;
  placeholder?: string;
  language?: string;
  onLanguageChange?: (language: string) => void;
  showLanguageSelector?: boolean;
  maxTags?: number;
  disabled?: boolean;
}

interface TagSuggestion {
  key?: string;
  tag?: string;
  name: string;
  category?: string;
  translations?: Record<string, string>;
  reason?: string;
}

const EnhancedTagAutocomplete: React.FC<EnhancedTagAutocompleteProps> = ({
  selectedTags,
  onTagsChange,
  placeholder = "Add tags...",
  language = 'en',
  onLanguageChange,
  showLanguageSelector = true,
  maxTags = 50,
  disabled = false
}) => {
  const [inputValue, setInputValue] = useState('');
  const [suggestions, setSuggestions] = useState<TagSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [availableLanguages, setAvailableLanguages] = useState<string[]>(['en', 'zh', 'ko', 'ja']);
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<NodeJS.Timeout>();

  // Load available languages on mount
  useEffect(() => {
    const loadLanguages = async () => {
      try {
        const response = await statsApi.getAvailableLanguages();
        setAvailableLanguages(response.data.languages || ['en', 'zh', 'ko', 'ja']);
      } catch (error) {
        console.error('Error loading languages:', error);
      }
    };
    loadLanguages();
  }, []);

  // Debounced search for suggestions
  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    if (inputValue.trim().length >= 2) {
      debounceRef.current = setTimeout(async () => {
        await fetchSuggestions(inputValue.trim());
      }, 300);
    } else {
      setSuggestions([]);
      setShowSuggestions(false);
    }

    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, [inputValue, language]);

  // Close suggestions when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        suggestionsRef.current &&
        !suggestionsRef.current.contains(event.target as Node) &&
        inputRef.current &&
        !inputRef.current.contains(event.target as Node)
      ) {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const fetchSuggestions = async (query: string) => {
    if (!query) return;

    setIsLoading(true);
    try {
      // Try service search first for structured tags
      const serviceResponse = await statsApi.serviceSearchTags(query, language, 10);
      const serviceSuggestions = serviceResponse.data.results || [];

      // Also try database search for user-added tags
      const dbResponse = await statsApi.searchTags(query, 10);
      const dbSuggestions = dbResponse.data.tags || [];

      // Combine and deduplicate suggestions
      const allSuggestions = [...serviceSuggestions, ...dbSuggestions];
      const uniqueSuggestions = allSuggestions.filter((suggestion, index, self) => 
        index === self.findIndex(s => (s.key || s.tag) === (suggestion.key || suggestion.tag))
      );

      setSuggestions(uniqueSuggestions);
      setShowSuggestions(true);
    } catch (error) {
      console.error('Error fetching suggestions:', error);
      setSuggestions([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleTagAdd = (tag: string) => {
    const tagKey = tag.toLowerCase().trim();
    if (tagKey && !selectedTags.includes(tagKey) && selectedTags.length < maxTags) {
      onTagsChange([...selectedTags, tagKey]);
    }
    setInputValue('');
    setShowSuggestions(false);
  };

  const handleTagRemove = (tagToRemove: string) => {
    onTagsChange(selectedTags.filter(tag => tag !== tagToRemove));
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (inputValue.trim() && !selectedTags.includes(inputValue.trim().toLowerCase())) {
        handleTagAdd(inputValue.trim());
      }
    } else if (e.key === 'Backspace' && !inputValue && selectedTags.length > 0) {
      handleTagRemove(selectedTags[selectedTags.length - 1]);
    } else if (e.key === 'Escape') {
      setShowSuggestions(false);
    }
  };

  const handleSuggestionClick = (suggestion: TagSuggestion) => {
    const tagKey = suggestion.key || suggestion.tag || suggestion.name;
    handleTagAdd(tagKey);
  };

  const getLanguageName = (lang: string) => {
    const names: Record<string, string> = {
      'en': 'English',
      'zh': '中文',
      'ko': '한국어',
      'ja': '日本語'
    };
    return names[lang] || lang.toUpperCase();
  };

  const getTagDisplayName = (suggestion: TagSuggestion) => {
    if (suggestion.translations && suggestion.translations[language]) {
      return suggestion.translations[language];
    }
    return suggestion.name;
  };

  return (
    <div className="relative">
      {/* Language Selector */}
      {showLanguageSelector && onLanguageChange && (
        <div className="mb-2 flex items-center space-x-2">
          <GlobeAltIcon className="h-4 w-4 text-gray-500" />
          <select
            value={language}
            onChange={(e) => onLanguageChange(e.target.value)}
            className="text-sm border border-gray-300 dark:border-gray-600 rounded px-2 py-1 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          >
            {availableLanguages.map(lang => (
              <option key={lang} value={lang}>
                {getLanguageName(lang)}
              </option>
            ))}
          </select>
        </div>
      )}

      {/* Tag Input */}
      <div className="flex flex-wrap items-center gap-2 p-3 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 focus-within:ring-2 focus-within:ring-blue-500 focus-within:border-transparent">
        {/* Selected Tags */}
        {selectedTags.map((tag) => (
          <span
            key={tag}
            className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200"
          >
            {tag}
            {!disabled && (
              <button
                type="button"
                onClick={() => handleTagRemove(tag)}
                className="ml-1 inline-flex items-center justify-center w-4 h-4 rounded-full hover:bg-blue-200 dark:hover:bg-blue-700 focus:outline-none"
              >
                <XMarkIcon className="h-3 w-3" />
              </button>
            )}
          </span>
        ))}

        {/* Input Field */}
        {selectedTags.length < maxTags && (
          <input
            ref={inputRef}
            type="text"
            value={inputValue}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            onFocus={() => inputValue.length >= 2 && setShowSuggestions(true)}
            placeholder={selectedTags.length === 0 ? placeholder : ''}
            disabled={disabled}
            className="flex-1 min-w-0 border-0 outline-none bg-transparent text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400"
          />
        )}

        {/* Loading Indicator */}
        {isLoading && (
          <SparklesIcon className="h-4 w-4 text-blue-500 animate-spin" />
        )}
      </div>

      {/* Suggestions Dropdown */}
      {showSuggestions && suggestions.length > 0 && (
        <div
          ref={suggestionsRef}
          className="absolute z-50 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-h-60 overflow-y-auto"
        >
          {suggestions.map((suggestion, index) => (
            <button
              key={`${suggestion.key || suggestion.tag || suggestion.name}-${index}`}
              type="button"
              onClick={() => handleSuggestionClick(suggestion)}
              className="w-full px-4 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 focus:bg-gray-100 dark:focus:bg-gray-700 focus:outline-none"
            >
              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium text-gray-900 dark:text-white">
                    {getTagDisplayName(suggestion)}
                  </div>
                  {suggestion.category && (
                    <div className="text-xs text-gray-500 dark:text-gray-400">
                      {suggestion.category}
                    </div>
                  )}
                  {suggestion.reason && (
                    <div className="text-xs text-blue-600 dark:text-blue-400">
                      {suggestion.reason}
                    </div>
                  )}
                </div>
                {suggestion.translations && (
                  <div className="flex space-x-1">
                    {Object.entries(suggestion.translations).slice(0, 3).map(([lang, text]) => (
                      <span
                        key={lang}
                        className="px-1 py-0.5 text-xs bg-gray-200 dark:bg-gray-600 rounded text-gray-600 dark:text-gray-300"
                      >
                        {text}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </button>
          ))}
        </div>
      )}

      {/* Helper Text */}
      <div className="mt-1 text-xs text-gray-500 dark:text-gray-400">
        {selectedTags.length}/{maxTags} tags
        {selectedTags.length > 0 && (
          <span className="ml-2">
            • Press Enter to add • Backspace to remove
          </span>
        )}
      </div>
    </div>
  );
};

export default EnhancedTagAutocomplete;
