import * as THREE from 'three';

let mixer;

export function createPositionTrack(times, values) {
    return new THREE.VectorKeyframeTrack('.position', times, values);
}

export function createQuaternionTrack(times, values) {
    return new THREE.QuaternionKeyframeTrack('.quaternion', times, values);
}

export function createClip(name, duration, tracks) {
    return new THREE.AnimationClip(name, duration, tracks);
}

export function createMixer(object) {
    mixer = new THREE.AnimationMixer(object);
}

export function createAction(clip) {
    return mixer.clipAction(clip);
}

export function playAction(action) {
    action.play();
}

export function updateMixer(delta) {
    if (mixer) {
        mixer.update(delta);
    }
}