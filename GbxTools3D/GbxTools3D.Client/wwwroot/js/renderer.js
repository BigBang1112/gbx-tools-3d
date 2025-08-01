﻿import * as THREE from 'three';
import { updateCamera, getControls } from './camera.js';
import { updateMixer } from './animation.js';
import { updateSlides } from './slide.js';

import { TransformControls } from 'three/addons/controls/TransformControls.js';

let renderer, scene, camera, stats, raycaster, raycasterEnabled, transformControls, dotNetHelper;

const pointer = new THREE.Vector2();
let INTERSECTED;

//THREE.Object3D.DEFAULT_MATRIX_AUTO_UPDATE = false;
//THREE.Object3D.DEFAULT_MATRIX_WORLD_AUTO_UPDATE = false;

const clock = new THREE.Clock();

export function create() {

    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setAnimationLoop(update);

    const canvas = document.querySelector("canvas");
    canvas.parentNode.replaceChild(renderer.domElement, canvas);

    window.addEventListener('resize', onWindowResize, false);

    raycaster = new THREE.Raycaster();
    window.addEventListener('mousemove', onPointerMove);

    //stats = new Stats();
    //document.body.appendChild(stats.dom);

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

    createTransformControls();
}

function onPointerMove(event) {
    pointer.x = (event.clientX / window.innerWidth) * 2 - 1;
    pointer.y = - (event.clientY / window.innerHeight) * 2 + 1;
}

export function enableRaycaster() {
    raycasterEnabled = true;
}

export function disableRaycaster() {
    raycasterEnabled = false;
}

export function createTransformControls() {
    if (!camera) {
        return;
    }

    transformControls = new TransformControls(camera, renderer.domElement);
    transformControls.setMode('rotate');
    transformControls.addEventListener('dragging-changed', function (event) {
        getControls().enabled = !event.value;
    });
    transformControls.size = 0.5;
}

export function attachTransformControls(obj) {
    if (transformControls) {
        transformControls.attach(obj);
    }
}

export function detachTransformControls() {
    if (transformControls) {
        transformControls.detach();
    }
}

export function showTransformControls() {
    if (scene && transformControls) {
        const helper = transformControls.getHelper();
        scene.remove(helper);
        scene.add(helper);
    }
}

export function hideTransformControls() {
    if (scene && transformControls) {
        scene.remove(transformControls.getHelper());
    }
}

export function setTransformControlsAxis(x, y, z) {
    if (!transformControls) {
        return;
    }
    transformControls.showX = x;
    transformControls.showY = y;
    transformControls.showZ = z;
}

export function passDotNet(helper) {
    dotNetHelper = helper;
}

export function dispose() {
    window.removeEventListener('resize', onWindowResize);

    //disposeInstances();
    disableRaycaster();
    hideTransformControls();

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
    if (stats) {
        stats.begin();
    }

    let delta = clock.getDelta();
    updateMixer(delta);
    
    if (scene && camera) {
        updateCamera(camera, delta);
        raycaster.setFromCamera(pointer, camera);

        if (raycasterEnabled) {
            const intersects = raycaster.intersectObjects(scene.children.filter(item => item.name !== "helper" && item.visible), true);
            processIntersections(intersects);
        }

        updateSlides(scene);

        renderer.render(scene, camera);
    }

    if (stats) {
        stats.end();
    }
}

function processIntersections(intersects) {
    const newIntersect = intersects.length > 0 ? intersects[0].object : null;

    if (newIntersect === INTERSECTED) return;

    if (INTERSECTED?.storedMaterial) {
        INTERSECTED.material = INTERSECTED.storedMaterial;
    }

    INTERSECTED = newIntersect;

    if (!INTERSECTED) return;

    if (INTERSECTED.material) {
        INTERSECTED.storedMaterial = INTERSECTED.material;

        INTERSECTED.material = INTERSECTED.material.clone();
        if (INTERSECTED.material.emissive) {
            INTERSECTED.material.emissive.setHex(0x666666);
        }

        dotNetHelper.invokeMethodAsync("Intersects", INTERSECTED.parent.id, INTERSECTED.material.name, INTERSECTED.material.userData);
    }
}

function onWindowResize() {
    // Update camera aspect ratio and renderer size
    if (camera) {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
    }
    renderer.setSize(window.innerWidth, window.innerHeight);
}