import * as THREE from 'three';
import { MapControls } from './addons/MapControls.js';

export function create() {
    const camera = new THREE.PerspectiveCamera(80, window.innerWidth / window.innerHeight, 0.1, 4096);
    camera.position.y = 2;
    camera.position.z = 5;
    camera.lookAt(0, 0, 0);
    camera.matrixAutoUpdate = true;
    camera.matrixWorldAutoUpdate = true;
    return camera;
}

export function createMapControls(camera, renderer, targetX, targetY, targetZ) {
    const controls = new MapControls(camera, renderer.domElement);

    controls.enableDamping = true; // an animation loop is required when either damping or auto-rotation are enabled
    controls.dampingFactor = 0.2;

    controls.screenSpacePanning = false;

    controls.minDistance = 1;
    controls.maxDistance = 2048;
    controls.zoomSpeed = 3;
    controls.maxPolarAngle = Math.PI / 2;
    controls.target.set(targetX, targetY, targetZ);
    
    return controls;
}

export function setPosition(camera, x, y, z) {
    camera.position.set(x, y, z);
}

export function lookAt(camera, x, y, z) {
    camera.lookAt(x, y, z);
}