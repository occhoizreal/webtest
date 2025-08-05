<template>
    <div class="w-full h-screen">
        <canvas
        ref="canvas"
        class="w-full h-full bg-gray-700"
        @mousedown="onMouseDown"
        @mousemove="onMouseMove"
        @mouseup="onMouseUp"
        @wheel="onWheel"
        ></canvas>
        <!-- Control panel -->
        <div 
        class="absolute top-4 left-1/2 transform 
            -translate-x-1/2 bg-gray-800 bg-opacity-90 p-3 
            rounded-lg flex gap-2 z-10"
        >
          <!-- Spawn control -->
          <div class="flex gap-2">
            <button 
                @click="selectTool('select')" 
                :class="['p-2 rounded transition-colors', currentTool === 'select' ? 'bg-blue-600' : 'bg-gray-600 hover:bg-gray-500']">
                <MousePointer class="w-4 h-4 text-white"/>
            </button>
            <button 
                @click="selectTool('move')" 
                :class="['p-2 rounded transition-colors', currentTool === 'move' ? 'bg-blue-600' : 'bg-gray-600 hover:bg-gray-500']">
                <Move class="w-4 h-4 text-white"/>
            </button>
            <button 
                @click="selectTool('scale')" 
                :class="['p-2 rounded transition-colors', currentTool === 'scale' ? 'bg-blue-600' : 'bg-gray-600 hover:bg-gray-500']">
                <Scaling class="w-4 h-4 text-white"/>
            </button>
            <button 
                @click="selectTool('rotate')" 
                :class="['p-2 rounded transition-colors', currentTool === 'rotate' ? 'bg-blue-600' : 'bg-gray-600 hover:bg-gray-500']">
                <Rotate3D class="w-4 h-4 text-white"/>
            </button>
            <button 
              @click="addCube" 
              class="p-2 rounded bg-green-600 hover:bg-green-500 transition-colors">
              <Plus class="w-4 h-4 text-white"/>
            </button>
          </div>
        </div>
        <!-- Hidden images that will be loaded as textures -->
        <div style="display: none;">
            <img ref="frontImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==" crossorigin="anonymous" />
            <img ref="backImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==" crossorigin="anonymous" />
            <img ref="topImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChAGA4849a8AAAAABJRU5ErkJggg==" crossorigin="anonymous" />
            <img ref="bottomImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPjPUAcAA2QB/+AIi64AAAAASUVORK5CYII=" crossorigin="anonymous" />
            <img ref="rightImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP4//8/AwAGAQL+lPqiYwAAAABJRU5ErkJggg==" crossorigin="anonymous" />
            <img ref="leftImage" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYGD4DwABBAEAcbhZIgAAAABJRU5ErkJggg==" crossorigin="anonymous" />
        </div>

        <!-- Controls panel (after cube list): -->
        <div v-if="selectedCube && currentTool === 'scale'" 
            class="absolute bottom-4 left-4 bg-gray-800 bg-opacity-90 p-3 rounded-lg z-10">
          <h3 class="text-white text-sm font-bold mb-2">Scale Controls</h3>
          <div class="space-y-2">
            <label class="text-white text-xs">
              X: <input type="range" min="0.1" max="3" step="0.1" 
                        v-model="selectedCube.scale[0]" class="ml-2">
            </label>
            <label class="text-white text-xs">
              Y: <input type="range" min="0.1" max="3" step="0.1" 
                        v-model="selectedCube.scale[1]" class="ml-2">
            </label>
            <label class="text-white text-xs">
              Z: <input type="range" min="0.1" max="3" step="0.1" 
                        v-model="selectedCube.scale[2]" class="ml-2">
            </label>
          </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, reactive } from 'vue';
import {Plus, Move, MousePointer, Scaling, Rotate3D} from 'lucide-vue-next';
import image from '../image/Box1.jpg'
const canvas = ref(null)
const frontImage = ref(null)
const backImage = ref(null)
const topImage = ref(null)
const bottomImage = ref(null)
const rightImage = ref(null)
const leftImage = ref(null)
const currentTool = ref('select')
const showRotationGizmo = ref(false)
const rotateAxis = ref(null)
const gizmoBuffers = {}
let wireframeProgram = null
let gl = null
let program = null
let buffers = {}
let textures = {}
let rotation = {x: 0, y: 0}
let zoom = -5
let isDragging = false
let lastMouseX = 0
let lastMouseY = 0
let animationId = null
const cubes = ref([
  { position: [0, 0, 0], scale: [1, 1, 1], rotation: [0, 0, 0], id: 1 }
])
const selectedCube = ref(null)
const keys = reactive({
  w: false, a: false, s: false, d: false
})

function selectTool(tool) {
  currentTool.value = tool
}

function onKeyDown(event) {
  const key = event.key.toLowerCase()
  if (['w', 'a', 's', 'd'].includes(key)) {
    keys[key] = true
    event.preventDefault()
  }
}

function onKeyUp(event) {
  const key = event.key.toLowerCase()
  if (['w', 'a', 's', 'd'].includes(key)) {
    keys[key] = false
    event.preventDefault()
  }
}

function handleKeyboardMovement() {
  const speed = 0.05
  if (keys.w) rotation.x -= speed
  if (keys.s) rotation.x += speed
  if (keys.a) rotation.y -= speed
  if (keys.d) rotation.y += speed
}

function addCube() {
  const newCube = {
    position: [Math.random() * 4 - 2, Math.random() * 4 - 2, Math.random() * 4 - 2],
    scale: [1, 1, 1],
    rotation: [0, 0, 0],
    id: Date.now()
  }
  cubes.value.push(newCube)
}

function selectCube(mouseX, mouseY) {
  if (!gl || !program) return

  const projectionMatrix = mat4Create()
  mat4Perspective(projectionMatrix, Math.PI/4, canvas.value.width/canvas.value.height, 0.1, 100.0)

  // Render scene to a framebuffer to pick cube
  const framebuffer = gl.createFramebuffer()
  gl.bindFramebuffer(gl.FRAMEBUFFER, framebuffer)

  const colorBuffer = gl.createRenderbuffer()
  gl.bindRenderbuffer(gl.RENDERBUFFER, colorBuffer)
  gl.renderbufferStorage(gl.RENDERBUFFER, gl.RGBA4, canvas.value.width, canvas.value.height)
  gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.RENDERBUFFER, colorBuffer)

  const depthBuffer = gl.createRenderbuffer()
  gl.bindRenderbuffer(gl.RENDERBUFFER, depthBuffer)
  gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16, canvas.value.width, canvas.value.height)
  gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT, gl.RENDERBUFFER, depthBuffer)

  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT)

  // Render each cube with a unique color
  cubes.value.forEach((cube, index) => {
    const r = ((index + 1) & 0xff) / 255
    const g = (((index + 1) >> 8) & 0xff) / 255
    const b = (((index + 1) >> 16) & 0xff) / 255

    gl.useProgram(wireframeProgram)
    let modelViewMatrix = mat4Create()
    mat4Identity(modelViewMatrix)
    mat4Translate(modelViewMatrix, modelViewMatrix, [0, 0, zoom])
    mat4RotateX(modelViewMatrix, modelViewMatrix, rotation.x)
    mat4RotateY(modelViewMatrix, modelViewMatrix, rotation.y)
    mat4Translate(modelViewMatrix, modelViewMatrix, cube.position)
    mat4RotateX(modelViewMatrix, modelViewMatrix, cube.rotation[0])
    mat4RotateY(modelViewMatrix, modelViewMatrix, cube.rotation[1])
    mat4RotateZ(modelViewMatrix, modelViewMatrix, cube.rotation[2])
    mat4Scale(modelViewMatrix, modelViewMatrix, cube.scale)

    gl.uniformMatrix4fv(wireframeProgram.uModelViewMatrix, false, modelViewMatrix)
    gl.uniformMatrix4fv(wireframeProgram.uProjectionMatrix, false, projectionMatrix)
    gl.uniform4f(wireframeProgram.uColor, r, g, b, 1.0)

    gl.bindBuffer(gl.ARRAY_BUFFER, buffers.position)
    gl.vertexAttribPointer(wireframeProgram.aPosition, 3, gl.FLOAT, false, 0, 0)
    gl.enableVertexAttribArray(wireframeProgram.aPosition)

    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffers.indices)
    gl.drawElements(gl.TRIANGLES, 36, gl.UNSIGNED_SHORT, 0)
  })

  // Read pixel under mouse
  const pixels = new Uint8Array(4)
  const x = mouseX
  const y = canvas.value.height - mouseY
  gl.readPixels(x, y, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, pixels)

  // Convert pixel color back to index
  const index = (pixels[0] | (pixels[1] << 8) | (pixels[2] << 16)) - 1
  selectedCube.value = index >= 0 && index < cubes.value.length ? cubes.value[index] : null

  // Clean up
  gl.deleteRenderbuffer(colorBuffer)
  gl.deleteRenderbuffer(depthBuffer)
  gl.deleteFramebuffer(framebuffer)
}

const wireframeVertexShaderSource = `
    attribute vec3 aPosition;
    uniform mat4 uModelViewMatrix;
    uniform mat4 uProjectionMatrix;
    void main() {
        gl_Position = uProjectionMatrix * uModelViewMatrix * vec4(aPosition, 1.0);
    }
`

const wireframeFragmentShaderSource = `
    precision mediump float;
    uniform vec4 uColor;
    void main() {
        gl_FragColor = uColor;
    }
`
const vertexShaderSource = `
    attribute vec3 aPosition;
    attribute vec3 aNormal;
    attribute vec2 aTexCoord;

    uniform mat4 uModelViewMatrix;
    uniform mat4 uProjectionMatrix;
    uniform mat4 uNormalMatrix;

    varying vec2 vTexCoord;
    varying vec3 vNormal;
    varying vec3 vPosition;

    void main(){
        vec4 position = uModelViewMatrix * vec4(aPosition, 1.0);
        gl_Position = uProjectionMatrix * position;

        vTexCoord = aTexCoord;
        vNormal = (uNormalMatrix * vec4(aNormal, 0.0)).xyz;
        vPosition = position.xyz;
    }
`

const fragmentShaderSource = `
    precision mediump float;

    varying vec2 vTexCoord;
    varying vec3 vNormal;
    varying vec3 vPosition;

    uniform sampler2D uTexture;
    uniform vec3 uLightDirection;

    void main(){
        vec3 normal = normalize(vNormal);
        vec3 lightDir = normalize(uLightDirection);

        float diffuse = max(dot(normal, lightDir), 0.0);
        float ambient = 0.4;

        vec4 textureColor = texture2D(uTexture, vTexCoord);
        vec3 color = textureColor.rgb * (ambient + diffuse * 0.6);
        gl_FragColor = vec4(color, textureColor.a);
    }
`

function createShader(gl, type, source){
    const shader = gl.createShader(type)
    gl.shaderSource(shader, source)
    gl.compileShader(shader)

    if(!gl.getShaderParameter(shader, gl.COMPILE_STATUS)){
        console.error('Error compiling shader:', gl.getShaderInfoLog(shader))
        return null
    }

    return shader
}

function createProgram(gl, vertexShader, fragmentShader){
    const program = gl.createProgram()
    gl.attachShader(program, vertexShader)
    gl.attachShader(program, fragmentShader)
    gl.linkProgram(program)

    if(!gl.getProgramParameter(program, gl.LINK_STATUS)){
        console.error('Error linking program:', gl.getProgramInfoLog(program))
        return null
    }
    return program
}

function loadImageTexture(gl, url) {
  const texture = gl.createTexture(gl, url)
  const image = new Image()
  
  image.onload = function() {
    gl.bindTexture(gl.TEXTURE_2D, texture)
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR)
  }
  image.src = url  
  return texture   
}   

// Create texture from image
function createTexture(gl, image) {
    const texture = gl.createTexture()
    gl.bindTexture(gl.TEXTURE_2D, texture)
    
    // Set parameters for texture wrapping and filtering
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR)
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR)
    
    // Upload image data to texture
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image)
    
    return texture
}

// Create cube geometry with texture coordinates
function createCube(){
    const vertices = [
        // Front face
        -1, -1,  1,   
        1, -1,  1,   
        1,  1,  1,  
        -1,  1,  1,
        // Back face
        -1, -1, -1, //bottom-left //4
        1, -1, -1, //bottom-right //7
        1, 1, -1, //top-right //6
        -1, 1, -1, //top-left //5
        
        //Top face
        -1, 1, -1, //back-left
        1, 1, -1, //back-right
        1, 1, 1, //front-right
        -1, 1, 1, //front-left
        
        

        // Bottom face
        -1, -1, -1,   
        1, -1, -1,   
        1, -1,  1,  
        -1, -1,  1,
        // Right face
        1, -1, 1, //front-bottom
        1, -1, -1, //back-bottom
        1, 1, -1, //back-top
        1, 1, 1, //front-top
        
        // Left face
        -1, -1, -1,  
        -1, -1,  1,  
        -1,  1,  1,  
        -1,  1, -1
    ]

    const normals = [
        // Front face
         0,  0,  1,   0,  0,  1,   0,  0,  1,   0,  0,  1,
        // Back face
         0,  0, -1,   0,  0, -1,   0,  0, -1,   0,  0, -1,
        // Top face
         0,  1,  0,   0,  1,  0,   0,  1,  0,   0,  1,  0,
        // Bottom face
         0, -1,  0,   0, -1,  0,   0, -1,  0,   0, -1,  0,
        // Right face
         1,  0,  0,   1,  0,  0,   1,  0,  0,   1,  0,  0,
        // Left face
        -1,  0,  0,  -1,  0,  0,  -1,  0,  0,  -1,  0,  0
    ]

    // Texture coordinates for each face
    const texCoords = [
        // Front face
        0, 0,  1, 0,  1, 1,  0, 1,
        // Back face
        0, 0,  1, 0,  1, 1,  0, 1,
        // Top face
        0, 0,  1, 0,  1, 1,  0, 1,
        // Bottom face
        0, 0,  1, 0,  1, 1,  0, 1,
        // Right face
        0, 0,  1, 0,  1, 1,  0, 1,
        // Left face
        0, 0,  1, 0,  1, 1,  0, 1
    ]

    const indices = [
         0,  1,  2,   0,  2,  3,    // front
         4,  5,  6,   4,  6,  7,    // back
         8,  9, 10,   8, 10, 11,    // top
        12, 13, 14,  12, 14, 15,    // bottom
        16, 17, 18,  16, 18, 19,    // right
        20, 21, 22,  20, 22, 23     // left
    ]
    
    return {vertices, normals, texCoords, indices}
}

function createRotationGizmo() {
  const segments = 32
  const radius = 1.2
  
  // X-axis circle (red)
  const xCircle = []
  for (let i = 0; i <= segments; i++) {
    const angle = (i / segments) * Math.PI * 2
    xCircle.push(0, Math.cos(angle) * radius, Math.sin(angle) * radius)
  }
  
  // Y-axis circle (green)  
  const yCircle = []
  for (let i = 0; i <= segments; i++) {
    const angle = (i / segments) * Math.PI * 2
    yCircle.push(Math.cos(angle) * radius, 0, Math.sin(angle) * radius)
  }
  
  // Z-axis circle (blue)
  const zCircle = []
  for (let i = 0; i <= segments; i++) {
    const angle = (i / segments) * Math.PI * 2
    zCircle.push(Math.cos(angle) * radius, Math.sin(angle) * radius, 0)
  }
  
  return { xCircle, yCircle, zCircle }
}

// Matrix operations
function mat4Create(){
    return new Float32Array(16)
}

function mat4Identity(out){
    out[0] = 1; out[1] = 0; out[2] = 0; out[3] = 0
    out[4] = 0; out[5] = 1; out[6] = 0; out[7] = 0
    out[8] = 0; out[9] = 0; out[10] = 1; out[11] = 0
    out[12] = 0; out[13] = 0; out[14] = 0; out[15] = 1
    return out
}

function mat4Perspective(out, fovy, aspect, near, far){
    const f = 1.0/Math.tan(fovy/2)
    out[0] = f/aspect; out[1] = 0; out[2] = 0; out[3] = 0
    out[4] = 0; out[5] = f; out[6] = 0; out[7] = 0
    out[8] = 0; out[9] = 0; out[10] = (far + near)/(near - far); out[11] = -1
    out[12] = 0; out[13] = 0; out[14] = (2 * far * near)/(near - far); out[15] = 0 
    return out
}

function mat4Translate(out, a, v) {
    out[0] = a[0]; out[1] = a[1]; out[2] = a[2]; out[3] = a[3]
    out[4] = a[4]; out[5] = a[5]; out[6] = a[6]; out[7] = a[7]
    out[8] = a[8]; out[9] = a[9]; out[10] = a[10]; out[11] = a[11]
    out[12] = a[0] * v[0] + a[4] * v[1] + a[8] * v[2] + a[12]
    out[13] = a[1] * v[0] + a[5] * v[1] + a[9] * v[2] + a[13]
    out[14] = a[2] * v[0] + a[6] * v[1] + a[10] * v[2] + a[14]
    out[15] = a[3] * v[0] + a[7] * v[1] + a[11] * v[2] + a[15]
    return out
}

function mat4RotateX(out, a, rad) {
    const s = Math.sin(rad)
    const c = Math.cos(rad)
    const a10 = a[4], a11 = a[5], a12 = a[6], a13 = a[7]
    const a20 = a[8], a21 = a[9], a22 = a[10], a23 = a[11]
    
    out[0] = a[0]; out[1] = a[1]; out[2] = a[2]; out[3] = a[3]
    out[4] = a10 * c + a20 * s
    out[5] = a11 * c + a21 * s
    out[6] = a12 * c + a22 * s
    out[7] = a13 * c + a23 * s
    out[8] = a20 * c - a10 * s
    out[9] = a21 * c - a11 * s
    out[10] = a22 * c - a12 * s
    out[11] = a23 * c - a13 * s
    out[12] = a[12]; out[13] = a[13]; out[14] = a[14]; out[15] = a[15]
    return out
}

function mat4RotateY(out, a, rad) {
    const s = Math.sin(rad)
    const c = Math.cos(rad)
    const a00 = a[0], a01 = a[1], a02 = a[2], a03 = a[3]
    const a20 = a[8], a21 = a[9], a22 = a[10], a23 = a[11]
    
    out[0] = a00 * c - a20 * s
    out[1] = a01 * c - a21 * s
    out[2] = a02 * c - a22 * s
    out[3] = a03 * c - a23 * s
    out[4] = a[4]; out[5] = a[5]; out[6] = a[6]; out[7] = a[7]
    out[8] = a00 * s + a20 * c
    out[9] = a01 * s + a21 * c
    out[10] = a02 * s + a22 * c
    out[11] = a03 * s + a23 * c
    out[12] = a[12]; out[13] = a[13]; out[14] = a[14]; out[15] = a[15]
    return out
}

// Create procedural textures (you can replace these with actual image URLs)
function createColorTexture(gl, r, g, b) {
    const canvas = document.createElement('canvas')
    canvas.width = 256
    canvas.height = 256
    const ctx = canvas.getContext('2d')
    
    // Create a gradient or pattern
    const gradient = ctx.createLinearGradient(0, 0, 256, 256)
    gradient.addColorStop(0, `rgb(${Math.floor(r*255)}, ${Math.floor(g*255)}, ${Math.floor(b*255)})`)
    gradient.addColorStop(1, `rgb(${Math.floor(r*128)}, ${Math.floor(g*128)}, ${Math.floor(b*128)})`)
    
    ctx.fillStyle = gradient
    ctx.fillRect(0, 0, 256, 256)
    
    // Add some pattern
    ctx.strokeStyle = `rgba(255, 255, 255, 0.2)`
    ctx.lineWidth = 2
    for (let i = 0; i < 256; i += 32) {
        ctx.beginPath()
        ctx.moveTo(i, 0)
        ctx.lineTo(i, 256)
        ctx.moveTo(0, i)
        ctx.lineTo(256, i)
        ctx.stroke()
    }
    
    return createTexture(gl, canvas)
}

function initWebGL(){
    gl = canvas.value.getContext('webgl')
    if(!gl){
        console.error('WebGL not supported')
        return false
    }

    const vertexShader = createShader(gl, gl.VERTEX_SHADER, vertexShaderSource)
    const fragmentShader = createShader(gl, gl.FRAGMENT_SHADER, fragmentShaderSource)
    const wireframeVertexShader = createShader(gl, gl.VERTEX_SHADER, wireframeVertexShaderSource)
    const wireframeFragmentShader = createShader(gl, gl.FRAGMENT_SHADER, wireframeFragmentShaderSource)

    if(!vertexShader || !fragmentShader || !wireframeVertexShader || !wireframeFragmentShader) return false

    program = createProgram(gl, vertexShader, fragmentShader)
    wireframeProgram = createProgram(gl, wireframeVertexShader, wireframeFragmentShader)
    if(!program || !wireframeProgram) return false

    program.aPosition = gl.getAttribLocation(program, 'aPosition')
    program.aNormal = gl.getAttribLocation(program, 'aNormal')
    program.aTexCoord = gl.getAttribLocation(program, 'aTexCoord')
    program.uModelViewMatrix = gl.getUniformLocation(program, 'uModelViewMatrix')
    program.uProjectionMatrix = gl.getUniformLocation(program, 'uProjectionMatrix')
    program.uNormalMatrix = gl.getUniformLocation(program, 'uNormalMatrix')
    program.uLightDirection = gl.getUniformLocation(program, 'uLightDirection')
    program.uTexture = gl.getUniformLocation(program, 'uTexture')

    wireframeProgram.aPosition = gl.getAttribLocation(wireframeProgram, 'aPosition')
    wireframeProgram.uModelViewMatrix = gl.getUniformLocation(wireframeProgram, 'uModelViewMatrix')
    wireframeProgram.uProjectionMatrix = gl.getUniformLocation(wireframeProgram, 'uProjectionMatrix')
    wireframeProgram.uColor = gl.getUniformLocation(wireframeProgram, 'uColor')

    const cube = createCube()

    buffers.position = gl.createBuffer()
    gl.bindBuffer(gl.ARRAY_BUFFER, buffers.position)
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(cube.vertices), gl.STATIC_DRAW)

    buffers.normal = gl.createBuffer()
    gl.bindBuffer(gl.ARRAY_BUFFER, buffers.normal)
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(cube.normals), gl.STATIC_DRAW)

    buffers.texCoord = gl.createBuffer()
    gl.bindBuffer(gl.ARRAY_BUFFER, buffers.texCoord)
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(cube.texCoords), gl.STATIC_DRAW)

    buffers.indices = gl.createBuffer()
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffers.indices)
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(cube.indices), gl.STATIC_DRAW)

    const mainTexture = loadImageTexture(gl, image)
    textures.front = mainTexture
    textures.back = mainTexture
    textures.top = mainTexture
    textures.bottom = mainTexture
    textures.right = mainTexture
    textures.left = mainTexture

    gl.enable(gl.DEPTH_TEST)
    gl.depthFunc(gl.LEQUAL)
    gl.clearColor(0.1, 0.1, 0.1, 1.0)

    return true
}

function mat4Scale(out, a, v) {
  out[0] = a[0] * v[0]; out[1] = a[1] * v[0]; out[2] = a[2] * v[0]; out[3] = a[3] * v[0];
  out[4] = a[4] * v[1]; out[5] = a[5] * v[1]; out[6] = a[6] * v[1]; out[7] = a[7] * v[1];
  out[8] = a[8] * v[2]; out[9] = a[9] * v[2]; out[10] = a[10] * v[2]; out[11] = a[11] * v[2];
  out[12] = a[12]; out[13] = a[13]; out[14] = a[14]; out[15] = a[15];
  return out;
}
function mat4RotateZ(out, a, rad) {
  const s = Math.sin(rad), c = Math.cos(rad);
  const a00 = a[0], a01 = a[1], a02 = a[2], a03 = a[3];
  const a10 = a[4], a11 = a[5], a12 = a[6], a13 = a[7];
  out[0] = a00 * c + a10 * s;
  out[1] = a01 * c + a11 * s;
  out[2] = a02 * c + a12 * s;
  out[3] = a03 * c + a13 * s;
  out[4] = a10 * c - a00 * s;
  out[5] = a11 * c - a01 * s;
  out[6] = a12 * c - a02 * s;
  out[7] = a13 * c - a03 * s;
  out[8] = a[8]; out[9] = a[9]; out[10] = a[10]; out[11] = a[11];
  out[12] = a[12]; out[13] = a[13]; out[14] = a[14]; out[15] = a[15];
  return out;
}

function render(){
  if(!gl || !program) return 

  const displayWidth = canvas.value.clientWidth
  const displayHeight = canvas.value.clientHeight

  if(canvas.value.width !== displayWidth || canvas.value.height !== displayHeight){
      canvas.value.width = displayWidth
      canvas.value.height = displayHeight
      gl.viewport(0, 0, displayWidth, displayHeight)
  }

  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT)
  gl.useProgram(program)

  const projectionMatrix = mat4Create()
  mat4Perspective(projectionMatrix, Math.PI/4, canvas.value.width/canvas.value.height, 0.1, 100.0)
  gl.uniformMatrix4fv(program.uProjectionMatrix, false, projectionMatrix)

  // Set up common attributes
  gl.bindBuffer(gl.ARRAY_BUFFER, buffers.position)
  gl.vertexAttribPointer(program.aPosition, 3, gl.FLOAT, false, 0, 0)
  gl.enableVertexAttribArray(program.aPosition)

  gl.bindBuffer(gl.ARRAY_BUFFER, buffers.normal)
  gl.vertexAttribPointer(program.aNormal, 3, gl.FLOAT, false, 0, 0)
  gl.enableVertexAttribArray(program.aNormal)

  gl.bindBuffer(gl.ARRAY_BUFFER, buffers.texCoord)
  gl.vertexAttribPointer(program.aTexCoord, 2, gl.FLOAT, false, 0, 0)
  gl.enableVertexAttribArray(program.aTexCoord)

  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffers.indices)
  gl.uniform3fv(program.uLightDirection, [0.5, 0.7, 1.0])

  // Render each cube
  cubes.value.forEach(cube => {
      let modelViewMatrix = mat4Create()
      let temp = mat4Create()
      let normalMatrix = mat4Create()

      mat4Identity(modelViewMatrix)
      mat4Translate(modelViewMatrix, modelViewMatrix, [0, 0, zoom])
      mat4RotateX(modelViewMatrix, modelViewMatrix, rotation.x)
      mat4RotateY(modelViewMatrix, modelViewMatrix, rotation.y)
      mat4Translate(modelViewMatrix, modelViewMatrix, cube.position)
      mat4RotateX(modelViewMatrix, modelViewMatrix, cube.rotation[0])
      mat4RotateY(modelViewMatrix, modelViewMatrix, cube.rotation[1])
      mat4RotateZ(modelViewMatrix, modelViewMatrix, cube.rotation[2])
      mat4Scale(modelViewMatrix, modelViewMatrix, cube.scale)

      for (let i = 0; i < 16; i++) {
          normalMatrix[i] = modelViewMatrix[i]
      }

      gl.uniformMatrix4fv(program.uModelViewMatrix, false, modelViewMatrix)
      gl.uniformMatrix4fv(program.uNormalMatrix, false, normalMatrix)

      // Draw cube faces
      const faceTextures = [
          textures.front, textures.back, textures.top,
          textures.bottom, textures.right, textures.left
      ]

      for (let i = 0; i < 6; i++) {
          gl.activeTexture(gl.TEXTURE0)
          gl.bindTexture(gl.TEXTURE_2D, faceTextures[i])
          gl.uniform1i(program.uTexture, 0)
          gl.drawElements(gl.TRIANGLES, 6, gl.UNSIGNED_SHORT, i * 6 * 2)
      }
      if (selectedCube.value && cube.id === selectedCube.value.id) {
        gl.useProgram(wireframeProgram)
        let wireframeMatrix = mat4Create()
        mat4Identity(wireframeMatrix)
        mat4Translate(wireframeMatrix, wireframeMatrix, [0, 0, zoom])
        mat4RotateX(wireframeMatrix, wireframeMatrix, rotation.x)
        mat4RotateY(wireframeMatrix, wireframeMatrix, rotation.y)
        mat4Translate(wireframeMatrix, wireframeMatrix, cube.position)
        mat4RotateX(wireframeMatrix, wireframeMatrix, cube.rotation[0])
        mat4RotateY(wireframeMatrix, wireframeMatrix, cube.rotation[1])
        mat4RotateZ(wireframeMatrix, wireframeMatrix, cube.rotation[2])
        mat4Scale(wireframeMatrix, wireframeMatrix, [cube.scale[0] * 1.05, cube.scale[1] * 1.05, cube.scale[2] * 1.05])

        gl.uniformMatrix4fv(wireframeProgram.uModelViewMatrix, false, wireframeMatrix)
        gl.uniformMatrix4fv(wireframeProgram.uProjectionMatrix, false, projectionMatrix)
        gl.uniform4f(wireframeProgram.uColor, 1.0, 1.0, 0.0, 1.0) // Yellow highlight

        gl.bindBuffer(gl.ARRAY_BUFFER, buffers.position)
        gl.vertexAttribPointer(wireframeProgram.aPosition, 3, gl.FLOAT, false, 0, 0)
        gl.enableVertexAttribArray(wireframeProgram.aPosition)

        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffers.indices)
        gl.lineWidth(2.0)
        // gl.drawElements(gl.LINES, 36, gl.UNSIGNED_SHORT, 0)
        const wireframeIndices = [
          0,1, 1,2, 2,3, 3,0, // front face
          4,5, 5,6, 6,7, 7,4, // back face  
          0,4, 1,5, 2,6, 3,7  // connecting edges
        ]
        const wireframeBuffer = gl.createBuffer()
        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, wireframeBuffer)
        gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(wireframeIndices), gl.STATIC_DRAW)
        gl.drawElements(gl.LINES, 24, gl.UNSIGNED_SHORT, 0)
      }
      // Add rotation gizmo rendering
      if (selectedCube.value && currentTool.value === 'rotate') {
        // Render rotation gizmo circles here
        const gizmo = createRotationGizmo()
        
        // Create gizmo buffers if they don't exist
        if (!gizmoBuffers.x) {
          gizmoBuffers.x = gl.createBuffer()
          gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.x)
          gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(gizmo.xCircle), gl.STATIC_DRAW)
          
          gizmoBuffers.y = gl.createBuffer()  
          gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.y)
          gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(gizmo.yCircle), gl.STATIC_DRAW)
          
          gizmoBuffers.z = gl.createBuffer()
          gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.z)  
          gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(gizmo.zCircle), gl.STATIC_DRAW)
        }
        
        gl.useProgram(wireframeProgram)
        
        // Draw X circle (red)
        gl.uniform4f(wireframeProgram.uColor, 1.0, 0.0, 0.0, 1.0)
        gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.x)
        gl.vertexAttribPointer(wireframeProgram.aPosition, 3, gl.FLOAT, false, 0, 0)
        gl.enableVertexAttribArray(wireframeProgram.aPosition)
        gl.drawArrays(gl.LINE_STRIP, 0, 33)
        
        // Draw Y circle (green)
        gl.uniform4f(wireframeProgram.uColor, 0.0, 1.0, 0.0, 1.0)
        gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.y)
        gl.vertexAttribPointer(wireframeProgram.aPosition, 3, gl.FLOAT, false, 0, 0)
        gl.enableVertexAttribArray(wireframeProgram.aPosition)
        gl.drawArrays(gl.LINE_STRIP, 0, 33)
        
        // Draw Z circle (blue)
        gl.uniform4f(wireframeProgram.uColor, 0.0, 0.0, 1.0, 1.0)
        gl.bindBuffer(gl.ARRAY_BUFFER, gizmoBuffers.z)
        gl.vertexAttribPointer(wireframeProgram.aPosition, 3, gl.FLOAT, false, 0, 0)
        gl.enableVertexAttribArray(wireframeProgram.aPosition)
        gl.drawArrays(gl.LINE_STRIP, 0, 33)
      }
  })
  handleKeyboardMovement()
  animationId = requestAnimationFrame(render)
}
function onMouseDown(event) {
  if (currentTool.value === 'select') {
    const rect = canvas.value.getBoundingClientRect()
    const mouseX = event.clientX - rect.left
    const mouseY = event.clientY - rect.top
    selectCube(mouseX, mouseY)
    return
  }
  if (currentTool.value === 'move' && selectedCube.value) {
    // Handle cube movement
    isDragging = true
    lastMouseX = event.clientX
    lastMouseY = event.clientY
  } 
  else if (currentTool.value === 'rotate' && selectedCube.value) {
    // Check which axis is clicked (approximate: X=red, Y=green, Z=blue)
    const rect = canvas.value.getBoundingClientRect()
    const cx = rect.left + rect.width / 2
    const cy = rect.top + rect.height / 2
    const dx = event.clientX - cx
    const dy = event.clientY - cy
    const r = Math.sqrt(dx*dx + dy*dy)
    if (Math.abs(r - 80) < 12) { // 80px radius, 12px tolerance
      // Approximate angle to axis
      const angle = Math.atan2(dy, dx)
      if (Math.abs(angle) < Math.PI/6) rotateAxis.value = 'x'
      else if (Math.abs(angle - Math.PI/2) < Math.PI/6) rotateAxis.value = 'y'
      else rotateAxis.value = 'z'
      isDragging = true
      lastMouseX = event.clientX
      lastMouseY = event.clientY
      return
    }
    // Otherwise, just rotate camera
    isDragging = true
    lastMouseX = event.clientX
    lastMouseY = event.clientY
  } 
  else {
    // Handle camera rotation (existing code)
    isDragging = true
    lastMouseX = event.clientX
    lastMouseY = event.clientY
  }
}

function onMouseMove(event) {
  if (!isDragging) return
  
  const deltaX = event.clientX - lastMouseX
  const deltaY = event.clientY - lastMouseY
  
  if (currentTool.value === 'move' && selectedCube.value) {
    if (event.shiftKey) {
      // Move along Y
      selectedCube.value.position[1] -= deltaY * 0.01
    } else if (event.altKey) {
      // Move along Z
      selectedCube.value.position[2] += deltaY * 0.01
    } else {
      // Move along X
      selectedCube.value.position[0] += deltaX * 0.01
    }
  } else if (currentTool.value === 'rotate' && selectedCube.value) {
    if (rotateAxis.value === 'x') selectedCube.value.rotation[0] += deltaY * 0.01
    if (rotateAxis.value === 'y') selectedCube.value.rotation[1] += deltaX * 0.01
    if (rotateAxis.value === 'z') selectedCube.value.rotation[2] += deltaX * 0.01
  } else {
    rotation.y += deltaX * 0.01
    rotation.x += deltaY * 0.01
  }
  
  lastMouseX = event.clientX
  lastMouseY = event.clientY
}


function onMouseUp() {
    isDragging = false
    rotateAxis.value = null
}

function onWheel(event) {
    zoom += event.deltaY * 0.01
    zoom = Math.max(-20, Math.min(-1, zoom))
    event.preventDefault()
}

onMounted(() => {
    if (initWebGL()) {
        render()
    }

    window.addEventListener('keydown', onKeyDown)
    window.addEventListener('keyup', onKeyUp)
})

onUnmounted(() => {
    if (animationId) {
        cancelAnimationFrame(animationId)
    }

    window.removeEventListener('keydown', onKeyDown)
    window.removeEventListener('keyup', onKeyUp)
})
</script>

<style scoped>
@import url('../styles/3dblock.css');
</style>