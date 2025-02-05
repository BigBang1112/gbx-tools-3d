import * as THREE from 'three';

export function get() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}