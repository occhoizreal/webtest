<template>
  <div class="container" :class="{ dark: isDark }">
    <!-- Sidebar -->
    <div class="sidebar">
      <div class="sidebar-header">
        <div class="logo">AI Assistant</div>
        <button class="theme-toggle" @click="toggleTheme">
          {{ isDark ? '☀️' : '🌙' }}
        </button>
      </div>
      
      <button class="new-chat-btn" @click="newChat">
        + New Chat
      </button>
      
      <div class="chat-history">
        <div 
          v-for="chat in chatHistory" 
          :key="chat.id"
          class="chat-item"
          @click="selectChat(chat)"
          :class="{ active: selectedChat?.id === chat.id }"
        >
          {{ chat.title }}
        </div>
      </div>
    </div>
    
    <!-- Main Chat Area -->
    <div class="chat-container">
      <div class="chat-header">
        <div class="ai-avatar">AI</div>
        <div class="chat-title">{{ selectedChat?.title || 'AI Assistant' }}</div>
      </div>
      
      <div class="chat-messages" ref="messagesContainer">
        <!-- Welcome Message -->
        <div v-if="messages.length === 0" class="welcome-message">
          <div class="welcome-title">Welcome to AI Assistant</div>
          <p>How can I help you today? Ask me anything!</p>
        </div>
        
        <!-- Messages -->
        <div 
          v-for="message in messages" 
          :key="message.id"
          class="message"
          :class="{ user: message.isUser, ai: !message.isUser }"
        >
          <div class="message-avatar">
            {{ message.isUser ? 'U' : 'AI' }}
          </div>
          <div class="message-content">
            {{ message.content }}
          </div>
        </div>
      </div>
      
      <!-- Typing Indicator -->
      <div v-if="isTyping" class="typing-indicator">
        <span>AI is thinking</span>
        <div class="typing-dots">
          <div class="typing-dot"></div>
          <div class="typing-dot"></div>
          <div class="typing-dot"></div>
        </div>
      </div>
      
      <!-- Input Area -->
      <div class="chat-input-container">
        <div class="chat-input-wrapper">
          <textarea 
            v-model="inputMessage"
            @keydown.enter.prevent="handleEnter"
            @input="autoResize"
            ref="chatInput"
            class="chat-input" 
            placeholder="Type your message here..." 
            rows="1"
          ></textarea>
          <button 
            class="send-btn" 
            @click="sendMessage"
            :disabled="!inputMessage.trim() || isTyping"
          >
            ➤
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, nextTick, onMounted } from 'vue'

// Reactive state
const isDark = ref(false)
const inputMessage = ref('')
const isTyping = ref(false)
const messages = ref([])
const selectedChat = ref(null)
const messagesContainer = ref(null)
const chatInput = ref(null)

// Chat history data
const chatHistory = reactive([
  { id: 1, title: 'Getting started with AI', messages: [] },
  { id: 2, title: 'Web development tips', messages: [] },
  { id: 3, title: 'Creative writing project', messages: [] },
  { id: 4, title: 'Data analysis help', messages: [] },
  { id: 5, title: 'Learning JavaScript', messages: [] }
])

// AI responses for simulation
const aiResponses = [
  "That's a great question! I'd be happy to help you with that. Let me provide you with a detailed response...",
  "I understand what you're asking. Here's my take on this topic, and I hope it helps clarify things for you.",
  "Thanks for reaching out! This is an interesting point you've raised. Let me break this down for you...",
  "I see what you mean. Based on my understanding, here's what I think would be most helpful for your situation...",
  "That's a thoughtful question. I'd recommend considering a few different approaches to this challenge..."
]

// Methods using const with arrow functions (Composition API style)
const toggleTheme = () => {
  isDark.value = !isDark.value
}

const autoResize = () => {
  if (chatInput.value) {
    chatInput.value.style.height = 'auto'
    chatInput.value.style.height = chatInput.value.scrollHeight + 'px'
  }
}

const scrollToBottom = async () => {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

const addMessage = async (content, isUser = false) => {
  const newMessage = {
    id: Date.now() + Math.random(),
    content,
    isUser,
    timestamp: new Date()
  }
  
  messages.value.push(newMessage)
  await scrollToBottom()
  
  // Save to current chat if selected
  if (selectedChat.value) {
    selectedChat.value.messages.push(newMessage)
  }
}

const simulateAIResponse = async () => {
  isTyping.value = true
  await scrollToBottom()
  
  // Simulate AI thinking time
  const delay = 1500 + Math.random() * 1000
  
  setTimeout(async () => {
    isTyping.value = false
    const randomResponse = aiResponses[Math.floor(Math.random() * aiResponses.length)]
    await addMessage(randomResponse, false)
  }, delay)
}

const sendMessage = async () => {
  const message = inputMessage.value.trim()
  if (!message || isTyping.value) return
  
  // Add user message
  await addMessage(message, true)
  
  // Clear input and reset height
  inputMessage.value = ''
  if (chatInput.value) {
    chatInput.value.style.height = 'auto'
  }
  
  // Simulate AI response
  await simulateAIResponse()
}

const handleEnter = (event) => {
  if (!event.shiftKey) {
    sendMessage()
  }
}

const newChat = () => {
  messages.value = []
  selectedChat.value = null
}

const selectChat = (chat) => {
  selectedChat.value = chat
  messages.value = [...chat.messages]
  scrollToBottom()
}

// Lifecycle
onMounted(() => {
  // Set initial focus
  if (chatInput.value) {
    chatInput.value.focus()
  }
})
</script>

<style scoped>
.container {
  display: flex;
  height: 100vh;
  max-width: 1200px;
  margin: 0 auto;
  box-shadow: 0 0 50px rgba(37, 99, 235, 0.1);
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background: #ffffff;
  color: #1e293b;
  transition: all 0.3s ease;
}

.container.dark {
  background: #0f172a;
  color: #f1f5f9;
}

/* Sidebar Styles */
.sidebar {
  width: 280px;
  background: #f8fafc;
  border-right: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;
}

.dark .sidebar {
  background: #1e293b;
  border-right-color: #334155;
}

.sidebar-header {
  padding: 20px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.dark .sidebar-header {
  border-bottom-color: #334155;
}

.logo {
  font-size: 18px;
  font-weight: bold;
  color: #2563eb;
}

.theme-toggle {
  background: none;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  padding: 8px;
  cursor: pointer;
  transition: all 0.3s ease;
  color: #1e293b;
}

.dark .theme-toggle {
  border-color: #334155;
  color: #f1f5f9;
}

.theme-toggle:hover {
  border-color: #2563eb;
  transform: scale(1.05);
}

.new-chat-btn {
  margin: 20px;
  padding: 12px 20px;
  background: #2563eb;
  color: white;
  border: none;
  border-radius: 12px;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.3s ease;
}

.new-chat-btn:hover {
  background: #1d4ed8;
  transform: translateY(-1px);
}

.chat-history {
  flex: 1;
  padding: 0 20px;
  overflow-y: auto;
}

.chat-item {
  padding: 12px 0;
  border-bottom: 1px solid #e2e8f0;
  color: #64748b;
  cursor: pointer;
  transition: all 0.3s ease;
}

.dark .chat-item {
  border-bottom-color: #334155;
  color: #94a3b8;
}

.chat-item:hover,
.chat-item.active {
  color: #2563eb;
  transform: translateX(5px);
}

/* Main Chat Area */
.chat-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: #ffffff;
}

.dark .chat-container {
  background: #0f172a;
}

.chat-header {
  padding: 20px 30px;
  border-bottom: 1px solid #e2e8f0;
  background: #ffffff;
  display: flex;
  align-items: center;
  gap: 15px;
}

.dark .chat-header {
  border-bottom-color: #334155;
  background: #0f172a;
}

.ai-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #2563eb, #3b82f6);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: bold;
}

.chat-title {
  font-size: 18px;
  font-weight: 600;
}

.chat-messages {
  flex: 1;
  padding: 30px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.message {
  display: flex;
  gap: 15px;
  max-width: 80%;
  animation: slideUp 0.3s ease;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.message.user {
  align-self: flex-end;
  flex-direction: row-reverse;
}

.message-avatar {
  width: 35px;
  height: 35px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  flex-shrink: 0;
}

.message.ai .message-avatar {
  background: linear-gradient(135deg, #2563eb, #3b82f6);
  color: white;
}

.message.user .message-avatar {
  background: #dbeafe;
  color: #2563eb;
}

.dark .message.user .message-avatar {
  background: #2563eb;
  color: white;
}

.message-content {
  background: #f8fafc;
  padding: 15px 20px;
  border-radius: 18px;
  line-height: 1.5;
  border: 1px solid #e2e8f0;
}

.dark .message-content {
  background: #1e293b;
  border-color: #334155;
}

.message.user .message-content {
  background: #2563eb;
  color: white;
  border-color: #2563eb;
}

.message.ai .message-content {
  border-left: 3px solid #2563eb;
}

.typing-indicator {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #64748b;
  font-style: italic;
  padding: 0 30px;
}

.dark .typing-indicator {
  color: #94a3b8;
}

.typing-dots {
  display: flex;
  gap: 3px;
}

.typing-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #2563eb;
  animation: typing 1.4s infinite ease-in-out;
}

.typing-dot:nth-child(1) { animation-delay: -0.32s; }
.typing-dot:nth-child(2) { animation-delay: -0.16s; }
.typing-dot:nth-child(3) { animation-delay: 0s; }

@keyframes typing {
  0%, 80%, 100% {
    transform: scale(0);
    opacity: 0.5;
  }
  40% {
    transform: scale(1);
    opacity: 1;
  }
}

.chat-input-container {
  padding: 20px 30px;
  border-top: 1px solid #e2e8f0;
  background: #ffffff;
}

.dark .chat-input-container {
  border-top-color: #334155;
  background: #0f172a;
}

.chat-input-wrapper {
  display: flex;
  gap: 15px;
  align-items: flex-end;
}

.chat-input {
  flex: 1;
  padding: 15px 20px;
  border: 2px solid #e2e8f0;
  border-radius: 25px;
  background: #f8fafc;
  color: #1e293b;
  font-size: 14px;
  resize: none;
  max-height: 120px;
  transition: all 0.3s ease;
}

.dark .chat-input {
  border-color: #334155;
  background: #1e293b;
  color: #f1f5f9;
}

.chat-input:focus {
  outline: none;
  border-color: #2563eb;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
}

.send-btn {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  background: #2563eb;
  color: white;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.send-btn:hover:not(:disabled) {
  background: #1d4ed8;
  transform: scale(1.05);
}

.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

.welcome-message {
  text-align: center;
  margin-top: 50px;
  color: #64748b;
}

.dark .welcome-message {
  color: #94a3b8;
}

.welcome-title {
  font-size: 24px;
  font-weight: 600;
  margin-bottom: 10px;
  color: #2563eb;
}

@media (max-width: 768px) {
  .sidebar {
    width: 250px;
  }
  
  .container {
    flex-direction: column;
  }
  
  .sidebar {
    width: 100%;
    height: auto;
    order: 2;
  }
  
  .chat-container {
    order: 1;
  }
}
</style>