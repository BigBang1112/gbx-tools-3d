import * as THREE from 'three';
import { MapControls } from './addons/MapControls.js';

let controls, followTarget, targetFar, targetUp, targetLookAtFactor;
//let leftPressed = false;
//let rightPressed = false;
//let bothPressed = false;

export function create(fov) {
    const camera = new THREE.PerspectiveCamera(fov, window.innerWidth / window.innerHeight, 0.1, 1024*64);
    camera.position.y = 2;
    camera.position.z = 5;
    camera.lookAt(0, 0, 0);
    return camera;
}

export function createMapControls(camera, renderer, targetX, targetY, targetZ) {
    if (controls) {
        controls.dispose();
    }

    controls = new MapControls(camera, renderer.domElement);

    controls.enableDamping = true; // an animation loop is required when either damping or auto-rotation are enabled
    controls.dampingFactor = 0.2;

    controls.minDistance = 1;
    controls.maxDistance = 2048;
    controls.zoomSpeed = 3;
    controls.maxPolarAngle = Math.PI / 2;
    controls.target.set(targetX, targetY, targetZ);

    /*renderer.domElement.addEventListener("mousedown", (event) => {
        if (event.button === 0) leftPressed = true; // Left button
        if (event.button === 2) rightPressed = true; // Right button

        bothPressed = leftPressed && rightPressed;
        controls.enablePan = !bothPressed;
        controls.enableRotate = !bothPressed;
    });

    renderer.domElement.addEventListener("mouseup", (event) => {
        if (event.button === 0) leftPressed = false;
        if (event.button === 2) rightPressed = false;
        
        bothPressed = leftPressed && rightPressed;
        controls.enablePan = !bothPressed;
        controls.enableRotate = !bothPressed;
    });

    renderer.domElement.addEventListener("mousemove", (event) => {
        if (bothPressed) {
            camera.position.y -= event.movementY;
            controls.target.y -= event.movementY;
        }
    });*/
    
    // set screenSpacePanning based on pressed shift key
    window.addEventListener("keydown", (event) => {
        controls.screenSpacePanning = event.shiftKey;
    });

    window.addEventListener("keyup", (event) => {
        controls.screenSpacePanning = event.shiftKey;
    });
}

export function setPosition(camera, x, y, z) {
    camera.position.set(x, y, z);
}

export function lookAt(camera, x, y, z) {
    camera.lookAt(x, y, z);
}

export function follow(target, far, up, lookAtFactor) {
    followTarget = target;
    targetFar = far;
    targetUp = up;
    targetLookAtFactor = lookAtFactor;
}

export function unfollow() {
    followTarget = null;
}

export function updateCamera(camera) {
    if (controls) {
        controls.update();
    }
    if (followTarget) {
        // Step 1: Determine the vehicle's forward direction.
        // We assume the vehicle's local forward is along the positive Z axis.
        const forward = new THREE.Vector3(0, 0, 1).applyQuaternion(followTarget.quaternion);

        // Project forward onto the horizontal plane (ignore vertical component)
        const horizontalForward = new THREE.Vector3(forward.x, 0, forward.z).normalize();

        // Step 2: Compute the camera's position.
        // Place the camera 'far' units behind the vehicle (opposite the horizontal forward)
        // and offset it upward by 'up' units.
        const cameraPosition = followTarget.position.clone().sub(horizontalForward.clone().multiplyScalar(targetFar));
        cameraPosition.y += targetUp;
        camera.position.copy(cameraPosition);

        // Step 3: Calculate the look-at target.
        // For lookAtValue = 0, the target is the vehicle's origin.
        // For lookAtValue = 1, the target is far ahead along the horizontal forward,
        // with no vertical offset (i.e. looking completely horizontal).
        const forwardTarget = followTarget.position.clone().add(horizontalForward.clone().multiplyScalar(targetFar));
        // Ensure the target remains at the vehicle's vertical level.
        forwardTarget.y = followTarget.position.y + targetUp;

        // Interpolate between the vehicle's position and the forward target.
        const lookAtTarget = new THREE.Vector3().lerpVectors(followTarget.position, forwardTarget, targetLookAtFactor);
        lookAtTarget.y -= 1; // manual tweak

        // Update the camera's rotation so that it looks at the computed target.
        camera.lookAt(lookAtTarget);
    }
}