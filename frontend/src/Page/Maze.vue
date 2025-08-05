<template>
    <div class="maze-container"> 
        <div ref="mazeCanvas" class="maze-canvas" tabindex="0"></div>
        <div class="coordinate">
            <div>
                Row: {{ mazePosition.row }}, Col: {{ mazePosition.col }}
            </div>
            <div>
                X: {{ curentPosition.x }}, Y: {{ curentPosition.y }}, Z: {{ curentPosition.z }}
            </div>
        </div>
        <div class="controls">
            <h3>Keybind Controls</h3>
            <p><strong>Mouse:</strong> Look around (click to enable)</p>
            <p><strong>WASD:</strong> Move</p>
            <p><strong>Space:</strong> Jumpp</p>
        </div>
    </div>
</template>

<script setup>
import * as THREE from 'three'
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import maze1 from '../maps/maze2.txt?raw'
import wall from '../image/wall.jpg'
import floor from '../image/floor.jpg'
import ceiling from '../image/ceiling1.jpg'
import walking from '../image/walking.mp3'
const mazeCanvas = ref(null)
let camera 
let scene 
let renderer
const keys = ref({})
const mouse = ref({x: 0, y: 0})
const isPointerLocked = ref(false)
const animationid = ref(null)
const mazePosition = ref({row: 0, col: 0})
const curentPosition = ref({x: 0, y: 0, z: 0})
let velocityY = 0
const gravity = -0.01
let jump = 0.14;
let isJumping = false
const groundLevel = 1
const moveSpeed = 0.1
const cellSize = 2
const wallHeight = 4
const playerRadius = 0.3
let walkingAudio = new Audio(walking)

// Fixed: Properly parse the maze data
const mazeRow = maze1
    .trim()
    .split('\n')
    .map(line => line.trim())
    .filter(Boolean)

const mazeHeight = mazeRow.length
const mazeWidth = mazeRow[0].length

const initMaze = () => {
    scene = new THREE.Scene()
    camera = new THREE.PerspectiveCamera(45, window.innerWidth/window.innerHeight, 0.1, 1000)
    renderer = new THREE.WebGLRenderer({antialias: true})
    renderer.setSize(window.innerWidth, window.innerHeight)
    renderer.setClearColor(0x87CEEB)
    renderer.shadowMap.enabled = true // Fixed: Enable shadow map
    renderer.shadowMap.type = THREE.PCFSoftShadowMap

    mazeCanvas.value.appendChild(renderer.domElement)

    setupLighting()
    createMaze()
    setStartPosition()
}

const setupLighting = () => {
    const ambientLighting = new THREE.AmbientLight(0x404040, 1.6)
    scene.add(ambientLighting)

    // Directional Light
    const directionalLight = new THREE.DirectionalLight(0xffffff, 2.2)
    directionalLight.position.set(50, 100, 50)
    directionalLight.castShadow = true
    directionalLight.shadow.mapSize.width = 2048
    directionalLight.shadow.mapSize.height = 2048
    directionalLight.shadow.camera.near = 0.5
    directionalLight.shadow.camera.far = 500
    directionalLight.shadow.camera.left = -100
    directionalLight.shadow.camera.right = 100
    directionalLight.shadow.camera.top = 100
    directionalLight.shadow.camera.bottom = -100
    scene.add(directionalLight)
}

const createMaze = () => {
    // Materials
    const textureLoader = new THREE.TextureLoader()
    const wallTexture = textureLoader.load(wall)
    const floorTexture = textureLoader.load(floor)
    const ceilingTexture = textureLoader.load(ceiling)
    const wallMaterial = new THREE.MeshLambertMaterial({map: wallTexture})
    const floorMaterial = new THREE.MeshLambertMaterial({map: floorTexture})
    const ceilingMaterial = new THREE.MeshLambertMaterial({map: ceilingTexture})

    const wallGeometry = new THREE.BoxGeometry(2, wallHeight, 2)
    const floorGeometry = new THREE.PlaneGeometry(2, 2)
    const ceilingGeometry = new THREE.PlaneGeometry(2, 2)
    const xOffset = Math.floor(mazeWidth/2)
    const zOffset = Math.floor(mazeHeight/2)
    for(let row = 0; row < mazeHeight; row++){
        for(let col = 0; col < mazeWidth; col++){
            const x = (col - xOffset) * cellSize
            const z = (row  - zOffset) * cellSize

            // Floor 
            const floor = new THREE.Mesh(floorGeometry, floorMaterial)
            floor.rotation.x = -Math.PI/2
            floor.position.set(x, -wallHeight/2, z) 
            floor.receiveShadow = true
            scene.add(floor)
            
            // Ceiling 
            const ceiling = new THREE.Mesh(ceilingGeometry, ceilingMaterial)
            ceiling.rotation.x = Math.PI/2
            ceiling.position.set(x, wallHeight/2, z) 
            scene.add(ceiling)
        }
    }

    // Walls
    for(let row = 0; row < mazeHeight; row++){
        for(let col = 0; col < mazeWidth; col++){
            if(mazeRow[row][col] === '1'){
                const wall = new THREE.Mesh(wallGeometry, wallMaterial)
                const x = (col - xOffset) * cellSize
                const z = (row - zOffset) * cellSize
                wall.position.set(x, 0, z)
                wall.castShadow = true
                wall.receiveShadow = true
                scene.add(wall)
            }
        }
    }
}

const setStartPosition = () => {
    const xOffset = Math.floor(mazeWidth/2)
    const zOffset = Math.floor(mazeHeight/2)
    let startX = 0, startZ = 0
    for(let row = 0; row < mazeHeight; row++){
        for(let col = 0; col < mazeWidth; col++){
            if(mazeRow[row][col] === '0'){
                startX = (col - xOffset) * cellSize
                startZ = (row - zOffset) * cellSize
                break
            }
        }
        if(startX !== 0 || startZ !== 0) break
    }
    camera.position.set(startX, 1, startZ)
}

const mazeCoordinator = (x, z) => {
    const xOffset = Math.floor(mazeWidth/2)
    const zOffset = Math.floor(mazeHeight/2)
    const col = Math.floor(x/cellSize + xOffset + 0.5)
    const row = Math.floor(z/cellSize + zOffset + 0.5)

    return {row, col}
}
const requestPointerLock = () => {
    renderer.domElement.requestPointerLock()
}

const onPointerLockChange = () => {
    isPointerLocked.value = document.pointerLockElement === renderer.domElement
}

const onMouseMove = (event) => {
    if(isPointerLocked.value){
        const sensitivity = 0.002
        mouse.value.x -= event.movementX * sensitivity
        mouse.value.y -= event.movementY * sensitivity
        mouse.value.y = Math.max(-Math.PI / 2, Math.min(Math.PI / 2, mouse.value.y))
    }
}

const onKeyDown = (event) => {
    if (isPointerLocked.value) {
        // Prevent scrolling with space, shift, arrows, etc.
        if (
            event.code === 'Space' ||
            event.code === 'ArrowUp' ||
            event.code === 'ArrowDown' ||
            event.code === 'ArrowLeft' ||
            event.code === 'ArrowRight' ||
            event.code === 'ShiftLeft' ||
            event.code === 'ShiftRight' ||
            event.code === 'KeyW' ||
            event.code === 'KeyA' ||
            event.code === 'KeyS' ||
            event.code === 'KeyD'
        ) {
            event.preventDefault()
        }
    }
    if(event.code === "Space" && !isJumping && camera.position.y <= groundLevel + 0.01){
        velocityY = jump
        isJumping = true
    }
    keys.value[event.code] = true
}

const onKeyUp = (event) => {
    event.preventDefault()
    keys.value[event.code] = false
}

const onWindowResize = () => {
    camera.aspect = window.innerWidth / window.innerHeight
    camera.updateProjectionMatrix()
    renderer.setSize(window.innerWidth, window.innerHeight)
}

const checkCollision = (x, z) => {
    const xOffset = Math.floor(mazeWidth / 2)
    const zOffset = Math.floor(mazeHeight / 2)
    
    // Use floor instead of round for more predictable grid mapping
    const centerCol = Math.floor(x / cellSize + xOffset + 0.5)
    const centerRow = Math.floor(z / cellSize + zOffset + 0.5)
    
    // Check the player's collision radius against nearby cells
    const radiusInCells = Math.ceil(playerRadius / cellSize)
    
    for(let dRow = -radiusInCells; dRow <= radiusInCells; dRow++) {
        for(let dCol = -radiusInCells; dCol <= radiusInCells; dCol++) {
            const checkRow = centerRow + dRow
            const checkCol = centerCol + dCol
            
            // Check bounds
            if(checkRow < 0 || checkRow >= mazeHeight || checkCol < 0 || checkCol >= mazeWidth) {
                return true // Collision with maze boundary
            }
            
            // Check if this cell is a wall
            if(mazeRow[checkRow][checkCol] === '1') {
                // Calculate the distance from player center to cell center
                const cellCenterX = (checkCol - xOffset) * cellSize
                const cellCenterZ = (checkRow - zOffset) * cellSize
                
                const dx = Math.abs(x - cellCenterX)
                const dz = Math.abs(z - cellCenterZ)
                
                // Check if the player's radius overlaps with this cell
                // Using a slightly larger collision box for the cell (cellSize/2 + buffer)
                const cellRadius = cellSize / 2 + 0.1
                
                if(dx < cellRadius + playerRadius && dz < cellRadius + playerRadius) {
                    return true 
                }
            }
        }
    }
    
    return false 
}

const updateMovement = () => {
    // Update camera rotation
    camera.rotation.order = "YXZ"
    camera.rotation.y = mouse.value.x
    camera.rotation.x = mouse.value.y
    
    // Movement vectors 
    const direction = new THREE.Vector3(0, 0, -1)
    direction.applyQuaternion(camera.quaternion)
    direction.y = 0 
    direction.normalize()

    const right = new THREE.Vector3(1, 0, 0)
    right.applyQuaternion(camera.quaternion)
    right.y = 0
    right.normalize()

    // Store current position
    const currentX = camera.position.x
    const currentZ = camera.position.z
    
    // Calculate desired movement
    let deltaX = 0
    let deltaZ = 0
    
    if(keys.value['KeyW']){
        deltaX += direction.x * moveSpeed
        deltaZ += direction.z * moveSpeed
    }
    if(keys.value['KeyA']){
        deltaX -= right.x * moveSpeed
        deltaZ -= right.z * moveSpeed
    }
    if(keys.value['KeyS']){
        deltaX -= direction.x * moveSpeed
        deltaZ -= direction.z * moveSpeed
    }
    if(keys.value['KeyD']){
        deltaX += right.x * moveSpeed
        deltaZ += right.z * moveSpeed
    }
    const isMoving = keys.value['KeyW'] || keys.value['KeyA'] || keys.value['KeyS'] || keys.value['KeyD']
    if(isMoving || isJumping) {
        if(walkingAudio.paused) {
            walkingAudio.loop = true
            walkingAudio.play().catch(e => console.log('Audio play failed:', e))
        }
    } else {
        if(!walkingAudio.paused) {
            walkingAudio.pause()
            walkingAudio.currentTime = 0
        }
    }
    // Try horizontal movement first (both X and Z together)
    const newX = currentX + deltaX
    const newZ = currentZ + deltaZ
    
    if(!checkCollision(newX, newZ)) {
        // Move both axes if no collision
        camera.position.x = newX
        camera.position.z = newZ
    } else {
        // If diagonal movement failed, try each axis separately
        
        // Try X movement only
        if(Math.abs(deltaX) > 0.001 && !checkCollision(currentX + deltaX, currentZ)) {
            camera.position.x = currentX + deltaX
        }
        
        // Try Z movement only (with potentially updated X position)
        if(Math.abs(deltaZ) > 0.001 && !checkCollision(camera.position.x, currentZ + deltaZ)) {
            camera.position.z = currentZ + deltaZ
        }
    }
    
    // Validate final position to prevent any teleporting
    if(checkCollision(camera.position.x, camera.position.z)) {
        console.warn("Invalid position detected, reverting to safe position")
        camera.position.x = currentX
        camera.position.z = currentZ
    }

    // Handle vertical movement (gravity and jumping)
    velocityY += gravity
    camera.position.y += velocityY

    if(camera.position.y <= groundLevel){
        camera.position.y = groundLevel
        velocityY = 0
        isJumping = false
    }

    // Update position display
    const mazeCoords = mazeCoordinator(camera.position.x, camera.position.z)
    mazePosition.value = {
        row: Math.max(0, Math.min(mazeHeight - 1, mazeCoords.row)),
        col: Math.max(0, Math.min(mazeWidth - 1, mazeCoords.col))
    }
    curentPosition.value = {
        x: Math.round(camera.position.x * 100)/100,
        y: Math.round(camera.position.y * 100)/100,
        z: Math.round(camera.position.z * 100)/100
    }
}

const animate = () => {
    animationid.value = requestAnimationFrame(animate)

    updateMovement()
    renderer.render(scene, camera)
}

const setupEventListeners = () => {
    // Pointer lock
    renderer.domElement.addEventListener('click', requestPointerLock)
    document.addEventListener('pointerlockchange', onPointerLockChange)

    // Mouse movement - Fixed: Should be 'mousemove', not 'mousedown'
    document.addEventListener('mousemove', onMouseMove)

    // Keyboard movement
    document.addEventListener('keydown', onKeyDown)
    document.addEventListener('keyup', onKeyUp)

    // Window resize
    window.addEventListener('resize', onWindowResize)
}

const cleanUp = () => {
    if(animationid.value){
        cancelAnimationFrame(animationid.value)
    }

    // Remove EventListeners
    document.removeEventListener('pointerlockchange', onPointerLockChange)
    document.removeEventListener('mousemove', onMouseMove)
    document.removeEventListener('keydown', onKeyDown)
    document.removeEventListener('keyup', onKeyUp)
    window.removeEventListener('resize', onWindowResize)

    if(renderer){
        renderer.dispose()
    }
}

onMounted(async() => {
    await nextTick()
    initMaze()
    setupEventListeners()
    animate()
})

onUnmounted(() => {
    cleanUp()
})
</script>

<style scoped>
@import url('../styles/maze.css');
</style>