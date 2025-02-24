import * as THREE from 'three';
import { updateCamera } from './camera.js';

let renderer, scene, camera, stats;

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
}

export function setCamera(newCamera) {
    camera = newCamera;
}


export function dispose() {
    window.removeEventListener('resize', onWindowResize);

    //disposeInstances();

    if (renderer) {
        renderer.dispose();
        if (renderer.domElement && renderer.domElement.parentNode) {
            renderer.domElement.parentNode.removeChild(renderer.domElement);
        }
    }
    scene = null;
    renderer = null;
    camera = null;
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
    if (camera) {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
    }
    renderer.setSize(window.innerWidth, window.innerHeight);
}