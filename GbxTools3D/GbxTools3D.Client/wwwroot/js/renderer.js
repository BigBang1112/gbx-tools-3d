import * as THREE from 'three';
import { updateCamera } from './camera.js';

let renderer, scene, camera, stats;
let directionalLight, shadowHelper;

export function create() {
    THREE.Object3D.DEFAULT_MATRIX_AUTO_UPDATE = false;
    THREE.Object3D.DEFAULT_MATRIX_WORLD_AUTO_UPDATE = false;

    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setAnimationLoop(update);

    const canvas = document.querySelector("canvas");
    canvas.parentNode.replaceChild(renderer.domElement, canvas);

    window.addEventListener('resize', onWindowResize, false);

    stats = new Stats();
    document.body.appendChild(stats.dom);

    return renderer;
}

export function getRenderer() {
    return renderer;
}

export function setScene(newScene) {
    scene = newScene;
    directionalLight = createDirectionalLight();
}

export function setCamera(newCamera) {
    camera = newCamera;
}

function update() {
    stats.begin();
    
    if (scene && camera) {
        updateCamera(camera);
        renderer.render(scene, camera);
    }
    stats.end();
}

function onWindowResize() {
    // Update camera aspect ratio and renderer size
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}

function createDirectionalLight() {
    THREE.Object3D.DEFAULT_MATRIX_AUTO_UPDATE = true;
    THREE.Object3D.DEFAULT_MATRIX_WORLD_AUTO_UPDATE = true;
    
    const light = new THREE.DirectionalLight(0xffffff, 1);

    light.position.set(3072, 2048, 3072); // Position the light in the scene
    light.target.position.set(0, 0, 0); // Point the light at the center

    //light.shadow.camera.position.set(1024*8, 1024, 1024*8); // Position the light in the scene
    //light.shadow.camera.lookAt(0, 0, 0); // Point the light at the center

    // Enable Shadows
    light.castShadow = true;

    // Adjust shadow map size for better quality
    light.shadow.mapSize.width = 4096;
    light.shadow.mapSize.height = 4096;

    // Set shadow camera parameters to cover the 1024x1024 scene
    light.shadow.camera.left = -2048;
    light.shadow.camera.right = 2048;
    light.shadow.camera.top = 2048;
    light.shadow.camera.bottom = -2048;
    light.shadow.camera.near = 512;
    light.shadow.camera.far = 5120;
    //light.shadow.autoUpdate = false;
    //light.shadow.needsUpdate = true;
    
    scene.add(light);

    //shadowHelper = new THREE.CameraHelper(light.shadow.camera);
    //scene.add(shadowHelper);

    THREE.Object3D.DEFAULT_MATRIX_AUTO_UPDATE = false;
    THREE.Object3D.DEFAULT_MATRIX_WORLD_AUTO_UPDATE = false;
    
    return light;
}