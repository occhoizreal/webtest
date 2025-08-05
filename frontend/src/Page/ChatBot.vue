<template>
  <div :class="isDarkMode ? 'dark-mode' : 'light-mode'" class="chatbot-container">
    <div class="chatbot-wrapper">
      <!-- Header -->
      <div class="header">
        <div class="header-left">
          <div class="avatar">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
            </svg>
          </div>
          <div class="header-info">
            <h3>AI Assistant</h3>
            <p class="status">{{ isOnline ? 'Online' : 'Offline' }}</p>
          </div>
        </div>
        
        <!-- Dark/Light Mode Toggle -->
        <button @click="toggleMode" class="mode-toggle">
          <svg v-if="isDarkMode" width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z"/>
          </svg>
          <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9c4.97 0 9-4.03 9-9 0-.46-.04-.92-.1-1.36-.98 1.37-2.58 2.26-4.4 2.26-2.98 0-5.4-2.42-5.4-5.4 0-1.81.89-3.42 2.26-4.4-.44-.06-.9-.1-1.36-.1z"/>
          </svg>
        </button>
      </div>

      <!-- Chat Messages Area -->
      <div class="messages-container" ref="messagesContainer">
        <div v-for="message in messages" :key="message.id" :class="['message', message.sender]">
          <div class="message-content">
            <div class="message-bubble">
              <div v-html="formatMessage(message.text)"></div>
            </div>
            <div class="message-time">
              {{ formatTime(message.timestamp) }}
            </div>
          </div>
        </div>
        
        <!-- Typing Indicator -->
        <div v-if="isTyping" class="message bot">
          <div class="message-content">
            <div class="message-bubble typing">
              <div class="typing-dots">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="input-container">
        <div class="input-wrapper">
          <input
            v-model="inputText"
            @keypress.enter="handleSendMessage"
            type="text"
            placeholder="Type your message..."
            class="message-input"
            :disabled="isTyping"
          />
          <button @click="handleSendMessage" :disabled="!inputText.trim() || isTyping" class="send-button">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted, getCurrentInstance } from 'vue'

const { proxy } = getCurrentInstance()

const isDarkMode = ref(false)
const inputText = ref('')
const isTyping = ref(false)
const isOnline = ref(false)
const conversationId = ref('default')
const messagesContainer = ref(null)
const messages = ref([
  {
    id: 1,
    text: "Hello! I'm your AI assistant. How can I help you today?",
    sender: 'bot',
    timestamp: new Date()
  }
])

onMounted(() => {
  checkBackendHealth()
  generateConversationId()
})

const toggleMode = () => {
  isDarkMode.value = !isDarkMode.value
}

const generateConversationId = () => {
  conversationId.value = 'conv_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9)
}

const checkBackendHealth = async () => {
  try {
    const response = await proxy.$axios.get('/Chat/health')
    if (response.status === 200) {
      isOnline.value = true
    } else {
      isOnline.value = false
    }
  } catch (error) {
    console.error('Backend health check failed:', error)
    isOnline.value = false
  }
}

const formatMessage = (text) => {
  return text
    .replace(/\n/g, '<br>')
    .replace(/\t/g, '&nbsp;&nbsp;&nbsp;&nbsp;')
    .replace(/\*/g, '•')
}

const handleSendMessage = async () => {
  if (!inputText.value.trim() || isTyping.value) return
  
  // Add user message
  const userMessage = {
    id: messages.value.length + 1,
    text: inputText.value.trim(),
    sender: 'user',
    timestamp: new Date()
  }
  messages.value.push(userMessage)
  
  const messageText = inputText.value.trim()
  inputText.value = ''
  isTyping.value = true
  
  scrollToBottom()
  
  try {
    console.log('Sending message:', messageText)
    
    // Send message to backend
    const response = await proxy.$axios.post('/Chat/message', {
      message: messageText,
      conversationId: conversationId.value
    })
    
    console.log('Full response:', response)
    console.log('Response data:', response.data)
    
    const data = response.data
    
    // Check if response has the expected structure
    if (!data) {
      console.error('No data in response:', response)
      throw new Error('No data received from server')
    }
    
    // Handle empty response from AI
    let responseText = data.response || 'I apologize, but I didn\'t generate a proper response. Please try again.'
    
    if (responseText.trim() === '') {
      responseText = 'I apologize, but I didn\'t generate a proper response. Please make sure Ollama is running with the llama3.1:8b model.'
    }
    
    // Add bot response
    const botResponse = {
      id: messages.value.length + 1,
      text: responseText,
      sender: 'bot',
      timestamp: new Date(data.timestamp || new Date()),
      sources: data.relevantSources || []
    }
    
    console.log('Adding bot response:', botResponse)
    messages.value.push(botResponse)
    
  } catch (error) {
    console.error('Error sending message:', error)
    console.error('Error response:', error.response)
    
    // Add error message
    const errorMessage = {
      id: messages.value.length + 1,
      text: "I'm sorry, I'm having trouble connecting to the server. Please make sure the backend is running and try again.",
      sender: 'bot',
      timestamp: new Date()
    }
    messages.value.push(errorMessage)
    
    isOnline.value = false
  } finally {
    isTyping.value = false
    scrollToBottom()
  }
}

const formatTime = (timestamp) => {
  return timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

const scrollToBottom = () => {
  nextTick(() => {
    const container = messagesContainer.value
    if (container) {
      container.scrollTop = container.scrollHeight
    }
  })
}
</script>

<style scoped>
@import url('../styles/chat-bot.css');
</style>