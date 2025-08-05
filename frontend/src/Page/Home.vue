<template>
  <div>
    <section class="hero-section bg-primary text-white py-5">
      <div class="container">
        <div class="row align-items-center min-vh-75">
          <div class="col-lg-6">
            <h1 class="display-4 fw-bold mb-4">
              Welcome to the amazing digital circus!
            </h1>
            <p class="lead mb-4">
              My name is Caine, and I'm your ringmaster and 
              I'm here to show you the most jaw-dropping, heart-stopping,
              mind-bending paraphernalia you've ever laid your eyes upon!
            </p>
            <div class="hero-button">
              <router-link to="/services" class="btn btn-light btn-lg me-3 mb-2">
                Our Services
              </router-link>
              <router-link to="/contact" class="btn btn-outline-light btn-lg mb-2">
                Contact Us
              </router-link>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="hero-image text-center">
              <img src="../image/home.jpg" alt="TADC Image" class="img-fluid rounded shadow">
            </div>
          </div>
        </div>
      </div>
    </section>
    <section class="features-section py-5">
      <div class="container">
        <div class="row text-center mb-5">
          <div class="col-12">
            <h2 class="display-5 fw-bold mb-3">Why Choose Us?</h2>
            <p class="lead text-muted">
              We offer the best solutions with cutting-edge technology and exceptional support
            </p>
          </div>
        </div>
        <div class="row g-4">
          <div v-for="feature in features" :key="feature.id" class="col-lg-4 col-md-6">
            <div class="feature-card h-100 p-4 text-center border rounded shadow-sm">
              <div class="feature-icon mb-3">
                <i :class="feature.icon" class="text-primary"></i>
              </div>
              <h4 class="fw-bold mb-3">{{ feature.title }}</h4>
              <p class="text-muted">{{ feature.description }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>
    <section class="stats-section bg-light py-5">
      <div class="container">
        <div class="row text-center g-4">
          <div
          v-for="stat in stats"
          :key="stat.id"
          class="col-lg-3 col-md-6"
          >
            <div class="stat-item">
              <h3 class="display-4 fw-bold text-primary mb-2">
                {{ animatedStats[stat.id] }}{{ stat.suffix }}
              </h3>
              <p class="text-muted fw-semibold">{{ stat.label }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>
    <section class="testimonials-section py-5">
      <div class="container">
        <div class="row text-centeer mb-5">
          <div class="col-12">
            <h2 class="display-5 fw-bold mb-3">
              What Our Clients Say
            </h2>
            <p class="lead text-muted">
              Don't just take our word for it - hear from our satisfied customers
            </p>
          </div>
        </div>
        <div class="row g-4">
          <div
          v-for="testimonial in testimonials"
          :key="testimonial.id"
          class="col-lg-4 col-md-6"
          >
            <div class="testimonial-card h-100 p-4 border rounded shadow-sm">
              <div class="stars mb-3">
                <i
                v-for="n in 5"
                :key="n"
                class="ri-star-fill text-warning"
                ></i>
              </div>
              <p class="mb-4 fst-italic">"{{ testimonial.comment }}"</p>
              <div class="testimonial-author d-flex align-items-center">
                <img
                :src="testimonial.avatar"
                :alt="testimonial.name"
                class="rounded-circle me-3"
                width="50"
                height="50"
                >
                <div>
                  <h6 class="mb-0 fw-bold">{{ testimonial.name }}</h6>
                  <small class="text-muted">{{ testimonial.position }}</small>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
    <section class="cta-section bg-dark text-white py-5">
      <div class="container text-center">
        <div class="row justify-content-center">
          <div class="col-lg-8">
            <h2 class="display-5 fw-bold mb-4">Ready to Get Started?</h2>
            <p class="lead mb-4">
              Join thousands of satisfied customers who trust us with their business needs.
            </p>
            <router-link to="/contact" class="btn btn-primary btn-lg">
              Contact Us Today
            </router-link>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
<script setup>
import {ref, onMounted, onUnmounted} from 'vue'
import trumpAvatar from '../image/Trump.jpg'
import elonAvatar from '../image/Elon.jpg'

const features=[
    {
    id: 1,
    icon: 'ri-rocket-line ri-3x',
    title: 'Fast & Reliable',
    description: 'Lightning-fast performance with 99.9% uptime guarantee. Your business never stops.'
    },
    {
    id: 2,
    icon: 'ri-shield-check-line ri-3x',
    title: 'Secure & Safe',
    description: 'Enterprise-grade security with end-to-end encryption to protect your data.'
    },
    {
    id: 3,
    icon: 'ri-customer-service-2-line ri-3x',
    title: '24/7 Support',
    description: 'Round-the-clock customer support to help you whenever you need assistance.'
    },
    {
    id: 4,
    icon: 'ri-settings-3-line ri-3x',
    title: 'Easy to Use',
    description: 'Intuitive interface designed for users of all technical levels. No learning curve.'
    },
    {
    id: 5,
    icon: 'ri-money-dollar-circle-line ri-3x',
    title: 'Cost Effective',
    description: 'Competitive pricing with flexible plans that grow with your business needs.'
    },
    {
    id: 6,
    icon: 'ri-global-line ri-3x',
    title: 'Global Reach',
    description: 'Worldwide coverage with local support in multiple languages and time zones.'
    }
]
const animatedStats = ref({
    1: 10,
    2: 0,
    3: 70,
    4: 0
})
const stats = [
    {
    id: 1,
    value: 1000,
    suffix: '+',
    label: 'Happy Customers'
    },
    {
    id: 2,
    value: 50,
    suffix: '+',
    label: 'Team Members'
    },
    {
    id: 3,
    value: 99,
    suffix: '%',
    label: 'Success Rate'
    },
    {
    id: 4,
    value: 5,
    suffix: '',
    label: 'Years Experience'
    }
]
const testimonials = [
    {
    id: 1,
    name: 'Donald Trump',
    position: 'The 45th and 47th President of the United States',
    comment: 'Outstanding service and support. They transformed our business operations completely.',
    avatar: trumpAvatar
    },
    {
    id: 2,
    name: 'Elon Musk',
    position: 'CEO of SpaceX and Tesla',
    comment: 'Professional, reliable, and always delivers on time. Highly recommended!',
    avatar: elonAvatar
    }
]
</script>
<style scoped>
@import url('../styles/home.css');
</style>