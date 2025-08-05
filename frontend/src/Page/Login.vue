<template>
    <div class="login-wrapper">
        <div class="login-container">
            <div class="logo">
                <h1>GameHub</h1>
                <p>Enter your gaming world</p>
            </div>
            <form id="loginForm">
                <table class="form-table">
                    <tr>
                        <td class="form-group">
                            <label for="email">Username or Email</label>
                            <input type="text" id="email" name="email" placeholder="Enter your username or email...">
                        </td>
                    </tr>
                    <tr>
                        <td class="form-group">
                            <label for="password">Password</label>
                            <input :type="isShowPassword ? 'text' : 'password'" id="password" name="password" placeholder="Enter your password...">
                            <div @click="showPassword">
                                <span v-if="isShowPassword" class="password-toggle"><EyeClosed/></span>
                                <span v-else class="password-toggle"><Eye/></span>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td class="remember-forgot-container">
                            <div class="remember-me">
                                <input type="checkbox" id="remember" name="remember">
                                <label for="remember">Remember me</label>
                            </div>
                            <div class="forgot-password">
                                <router-link to="/forgot-password">Forgot Password</router-link>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <button type="submit" class="login-btn">Log In</button>
                    </tr>
                </table>
            </form>

            <div class="divider">
                <span>Or continue with</span>
            </div>

            <div class="social-login">
                <button class="social-btn" onclick="socialLogin('google')">
                    <span><font-awesome-icon :icon="['fab', 'google']" /></span> Google
                </button>
                <button class="social-btn" onclick="socialLogin('discord')">
                    <span></span> Discord
                </button>
            </div>
        </div>
    </div>
</template>
<script setup>
import { ref } from 'vue';
import { Eye, EyeClosed } from 'lucide-vue-next';
const isShowPassword = ref(false)

const showPassword = () => {
    isShowPassword.value = !isShowPassword.value
}
</script>
<style scoped>
@import url('../styles/login.css');
</style>