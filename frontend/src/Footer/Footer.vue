<template>
  <footer class="footer bg-dark text-white">
    <div class="container">
      <div class="row py-4">
        <div class="col-lg-4 col-md-6 mb-4">
          <h5>Company Name</h5>
          <p class="text-muted">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. 
            Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
          </p>
        </div>
        
        <div class="col-lg-2 col-md-6 mb-4">
          <h6>Quick Links</h6>
          <ul class="list-unstyled">
            <li>
              <router-link to="/" class="text-muted text-decoration-none">
                Home
              </router-link>
            </li>
            <li>
              <router-link to="/about" class="text-muted text-decoration-none">
                About
              </router-link>
            </li>
            <li>
              <router-link to="/services" class="text-muted text-decoration-none">
                Services
              </router-link>
            </li>
            <li>
              <router-link to="/contact" class="text-muted text-decoration-none">
                Contact
              </router-link>
            </li>
          </ul>
        </div>
        
        <div class="col-lg-3 col-md-6 mb-4">
          <h6>Contact Info</h6>
          <div class="text-muted">
            <p><i class="ri-map-pin-line"></i> 123 Street Name, City</p>
            <p><i class="ri-phone-line"></i> +1 234 567 8900</p>
            <p><i class="ri-mail-line"></i> info@company.com</p>
          </div>
        </div>
        
        <div class="col-lg-3 col-md-6 mb-4">
          <h6>Follow Us</h6>
          <div class="social-links">
            <a href="#" class="text-muted me-3">
              <i class="ri-facebook-fill"></i>
            </a>
            <a href="#" class="text-muted me-3">
              <i class="ri-twitter-fill"></i>
            </a>
            <a href="#" class="text-muted me-3">
              <i class="ri-instagram-fill"></i>
            </a>
            <a href="#" class="text-muted">
              <i class="ri-linkedin-fill"></i>
            </a>
          </div>
        </div>
      </div>
      
      <hr class="border-secondary">
      
      <div class="row py-3">
        <div class="col-md-6">
          <p class="text-muted mb-0">
            &copy; {{ currentYear }} Company Name. All rights reserved.
          </p>
        </div>
        <div class="col-md-6 text-md-end">
          <p class="text-muted mb-0">
            <router-link to="/privacy" class="text-muted text-decoration-none me-3">
              Privacy Policy
            </router-link>
            <router-link to="/terms" class="text-muted text-decoration-none">
              Terms of Service
            </router-link>
          </p>
        </div>
      </div>
    </div>
  </footer>
</template>

<script>
import { computed } from 'vue'

export default {
  name: 'Footer',
  setup() {
    const currentYear = computed(() => {
      return new Date().getFullYear()
    })
    
    return {
      currentYear
    }
  }
}
</script>

<style scoped>
.footer {
  margin-top: auto;
}

.social-links a {
  font-size: 1.5rem;
  transition: color 0.3s ease;
}

.social-links a:hover {
  color: #007bff !important;
}

.footer a:hover {
  color: #007bff !important;
}
</style>