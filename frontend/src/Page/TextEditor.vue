<template>
  <div :class="['min-h-screen transition-colors duration-300', isDarkMode ? 'bg-gray-900' : 'bg-gray-50']">
    <div class="max-w-7xl mx-auto p-4 md:p-8">
      <!-- Header -->
      <div class="flex justify-between items-center mb-8">
        <div class="flex items-center gap-3">
          <Sparkles :class="['w-8 h-8', isDarkMode ? 'text-purple-400' : 'text-purple-600']" />
          <h1 :class="['text-3xl font-bold', isDarkMode ? 'text-white' : 'text-gray-900']">
            AI Writing Assistant
          </h1>
        </div>
        <button
          @click="isDarkMode = !isDarkMode"
          :class="[
            'p-2 rounded-lg transition-colors',
            isDarkMode ? 'bg-gray-800 hover:bg-gray-700 text-yellow-400' : 'bg-gray-200 hover:bg-gray-300 text-gray-700'
          ]"
        >
          <component :is="isDarkMode ? Sun : Moon" class="w-5 h-5" />
        </button>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Editor Panel -->
        <div :class="['rounded-xl shadow-lg p-6', isDarkMode ? 'bg-gray-800' : 'bg-white']">
          <div class="flex justify-between items-center mb-4">
            <h2 :class="['text-xl font-semibold', isDarkMode ? 'text-white' : 'text-gray-900']">
              Your Text
            </h2>
            <div class="flex gap-2">
              <button
                @click="loadSampleText"
                :class="[
                  'px-3 py-1 text-sm rounded-lg transition-colors flex items-center gap-1',
                  isDarkMode ? 'bg-gray-700 hover:bg-gray-600 text-gray-300' : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
                ]"
              >
                <FileText class="w-4 h-4" />
                Sample
              </button>
              <button
                @click="copyText"
                :class="[
                  'px-3 py-1 text-sm rounded-lg transition-colors flex items-center gap-1',
                  isDarkMode ? 'bg-gray-700 hover:bg-gray-600 text-gray-300' : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
                ]"
              >
                <Copy class="w-4 h-4" />
                Copy
              </button>
            </div>
          </div>

          <textarea
            ref="textareaRef"
            v-model="text"
            placeholder="Enter your text here..."
            :class="[
              'w-full h-96 p-4 rounded-lg border transition-colors resize-none focus:outline-none focus:ring-2',
              isDarkMode
                ? 'bg-gray-900 border-gray-700 text-white placeholder-gray-500 focus:ring-purple-500'
                : 'bg-gray-50 border-gray-300 text-gray-900 placeholder-gray-400 focus:ring-purple-400'
            ]"
          ></textarea>

          <div class="mt-4 flex justify-between items-center">
            <span :class="['text-sm', isDarkMode ? 'text-gray-400' : 'text-gray-600']">
              {{ text.length }} characters
            </span>
            <button
              @click="analyzeText"
              :disabled="isAnalyzing || !text.trim()"
              :class="[
                'px-6 py-2 rounded-lg font-medium transition-all transform hover:scale-105 flex items-center gap-2',
                isAnalyzing || !text.trim()
                  ? 'bg-gray-600 text-gray-400 cursor-not-allowed'
                  : 'bg-gradient-to-r from-purple-500 to-indigo-600 text-white hover:from-purple-600 hover:to-indigo-700 shadow-lg'
              ]"
            >
              <template v-if="isAnalyzing">
                <Loader2 class="w-4 h-4 animate-spin" />
                Analyzing...
              </template>
              <template v-else>
                <Sparkles class="w-4 h-4" />
                Analyze Text
              </template>
            </button>
          </div>

          <div v-if="error" class="mt-4 p-3 rounded-lg bg-red-500/10 border border-red-500/20">
            <p class="text-red-500 text-sm">{{ error }}</p>
          </div>
        </div>

        <!-- Suggestions Panel -->
        <div :class="['rounded-xl shadow-lg p-6', isDarkMode ? 'bg-gray-800' : 'bg-white']">
          <h2 :class="['text-xl font-semibold mb-4', isDarkMode ? 'text-white' : 'text-gray-900']">
            Suggestions
          </h2>

          <!-- Category Filter -->
          <div class="flex flex-wrap gap-2 mb-4">
            <button
              v-for="category in categories"
              :key="category.id"
              @click="activeCategory = category.id"
              :class="[
                'px-3 py-1 rounded-full text-sm font-medium transition-all',
                activeCategory === category.id
                  ? `${category.color} text-white`
                  : isDarkMode
                    ? 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              ]"
            >
              {{ category.label }}
              <span v-if="suggestions.filter(s => category.id === 'all' || s.category === category.id).length > 0" class="ml-1">
                ({{ suggestions.filter(s => category.id === 'all' || s.category === category.id).length }})
              </span>
            </button>
          </div>

          <div class="space-y-3 max-h-96 overflow-y-auto">
            <div
              v-if="filteredSuggestions.length === 0"
              :class="['text-center py-12', isDarkMode ? 'text-gray-500' : 'text-gray-400']"
            >
              {{ suggestions.length === 0 ? "Click 'Analyze Text' to get suggestions" : "No suggestions in this category" }}
            </div>
            <div
              v-else
              v-for="(suggestion, index) in filteredSuggestions"
              :key="index"
              :class="[
                'p-4 rounded-lg border transition-all hover:shadow-md',
                isDarkMode ? 'bg-gray-900 border-gray-700 hover:border-gray-600' : 'bg-gray-50 border-gray-200 hover:border-gray-300'
              ]"
            >
              <div class="flex justify-between items-start mb-2">
                <span :class="['inline-block px-2 py-1 rounded-full text-xs font-medium text-white', getCategoryColor(suggestion.category)]">
                  {{ suggestion.category }}
                </span>
                <div class="flex gap-1">
                  <button
                    @click="applySuggestion(suggestion)"
                    class="p-1 rounded hover:bg-green-500/20 text-green-500 transition-colors"
                    title="Apply suggestion"
                  >
                    <Check class="w-4 h-4" />
                  </button>
                  <button
                    @click="suggestions = suggestions.filter(s => s !== suggestion)"
                    class="p-1 rounded hover:bg-red-500/20 text-red-500 transition-colors"
                    title="Dismiss"
                  >
                    <X class="w-4 h-4" />
                  </button>
                </div>
              </div>

              <div class="space-y-2">
                <div class="flex items-center gap-2 text-sm">
                  <span :class="['line-through', isDarkMode ? 'text-red-400' : 'text-red-600']">
                    {{ suggestion.issue }}
                  </span>
                  <span :class="[isDarkMode ? 'text-gray-500' : 'text-gray-400']">→</span>
                  <span :class="['font-medium', isDarkMode ? 'text-green-400' : 'text-green-600']">
                    {{ suggestion.suggestion }}
                  </span>
                </div>

                <p :class="['text-sm', isDarkMode ? 'text-gray-400' : 'text-gray-600']">
                  {{ suggestion.explanation }}
                </p>
              </div>
            </div>
          </div>

          <div v-if="suggestions.length > 0" :class="['mt-4 pt-4 border-t', isDarkMode ? 'border-gray-700' : 'border-gray-200']">
            <button
              @click="applyAllSuggestions"
              class="w-full py-2 rounded-lg bg-gradient-to-r from-green-500 to-emerald-600 text-white font-medium hover:from-green-600 hover:to-emerald-700 transition-all"
            >
              Apply All Suggestions
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { Sparkles, Check, X, Loader2, Sun, Moon, Copy, FileText } from 'lucide-vue-next';

const text = ref('');
const suggestions = ref([]);
const isAnalyzing = ref(false);
const error = ref('');
const isDarkMode = ref(true);
const activeCategory = ref('all');
const textareaRef = ref(null);

const categories = [
  { id: 'all', label: 'All', color: 'bg-purple-500' },
  { id: 'grammar', label: 'Grammar', color: 'bg-blue-500' },
  { id: 'spelling', label: 'Spelling', color: 'bg-red-500' },
  { id: 'punctuation', label: 'Punctuation', color: 'bg-yellow-500' },
  { id: 'style', label: 'Style', color: 'bg-green-500' },
  { id: 'clarity', label: 'Clarity', color: 'bg-indigo-500' }
];

const sampleTexts = [
  "The quick brown fox jumps over the lazy dog. This sentence contains all letters of the alphabet.",
  "Their going to there house over they're after the meeting concludes tomorrow morning.",
  "The implementation of the aforementioned methodological approach necessitates a comprehensive reevaluation of the existing paradigmatic frameworks.",
  "she dont know weather to go too the store or not the decision is dificult"
];

const analyzeText = async () => {
  if (!text.value.trim()) {
    error.value = 'Please enter some text to analyze';
    return;
  }

  isAnalyzing.value = true;
  error.value = '';
  suggestions.value = [];

  try {
    const prompt = `Analyze the following text and provide specific suggestions for improvement. Focus on grammar, spelling, punctuation, style, and clarity. 

Text to analyze:
"${text.value}"

Respond with a JSON array of suggestion objects. Each object should have:
- category: one of "grammar", "spelling", "punctuation", "style", or "clarity"
- issue: the specific text that needs improvement
- suggestion: the corrected or improved version
- explanation: a brief explanation of why this change improves the text
- position: approximate starting position in the text (character index)

Only include actual issues that need correction. If the text is perfect, return an empty array.

Your entire response must be a valid JSON array. DO NOT include any text outside the JSON structure.`;

    const response = await window.claude.complete(prompt);

    try {
      const parsedSuggestions = JSON.parse(response);
      if (Array.isArray(parsedSuggestions)) {
        suggestions.value = parsedSuggestions;
      } else {
        throw new Error('Invalid response format');
      }
    } catch (parseError) {
      console.error('Parse error:', parseError);
      error.value = 'Failed to parse suggestions. Please try again.';
    }
  } catch (err) {
    console.error('Analysis error:', err);
    error.value = 'Failed to analyze text. Please try again.';
  } finally {
    isAnalyzing.value = false;
  }
};

const applySuggestion = (suggestion) => {
  text.value = text.value.replace(suggestion.issue, suggestion.suggestion);
  suggestions.value = suggestions.value.filter(s => s !== suggestion);
};

const filteredSuggestions = computed(() => {
  return activeCategory.value === 'all'
    ? suggestions.value
    : suggestions.value.filter(s => s.category === activeCategory.value);
});

const getCategoryColor = (category) => {
  const cat = categories.find(c => c.id === category);
  return cat ? cat.color : 'bg-gray-500';
};

const copyText = () => {
  navigator.clipboard.writeText(text.value);
};

const loadSampleText = () => {
  const randomSample = sampleTexts[Math.floor(Math.random() * sampleTexts.length)];
  text.value = randomSample;
};

const applyAllSuggestions = () => {
  suggestions.value.forEach(s => applySuggestion(s));
};
</script>