import * as THREE from 'three';

const textureLoader = new THREE.TextureLoader();

export function get() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}

export function getWithTexture(texture) {
    return new THREE.MeshStandardMaterial({ map: texture });
}

export function loadTexture(path) {
    const texture = textureLoader.load(path);
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    return texture;
}