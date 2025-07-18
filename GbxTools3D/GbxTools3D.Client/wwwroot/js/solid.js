import * as THREE from 'three';
import * as BufferGeometryUtils from 'three/addons/utils/BufferGeometryUtils.js';
import { VertexNormalsHelper } from 'three/addons/helpers/VertexNormalsHelper.js';
import { Earcut } from 'three/src/extras/Earcut.js';
//import { InstancedMesh2 } from '@three.ez/instanced-mesh';

const collisionMaterial = new THREE.MeshBasicMaterial({ wireframe: true, color: 0x00ff00 });

export function create(matrixAutoUpdate) {
    var obj = new THREE.Object3D();
    obj.matrixAutoUpdate = matrixAutoUpdate;
    obj.matrixWorldAutoUpdate = matrixAutoUpdate;
    return obj;
}

export function setName(tree, name) {
    tree.name = name;
}

export function setUserData(tree, filePath) {
    tree.userData.filePath = filePath;
}

export function getObjectByName(tree, name) {
    return tree.getObjectByName(name);
}

export function reorderEuler(tree) {
    tree.rotation.reorder('YXZ');
}

export function add(parent, child) {
    parent.add(child);
}

export function setPosition(tree, x, y, z) {
    tree.position.set(x, y, z);
}

export function setRotationMatrix(tree, xx, xy, xz, yx, yy, yz, zx, zy, zz) {
    tree.setRotationFromMatrix(new THREE.Matrix4().set(xx, xy, xz, 0, yx, yy, yz, 0, zx, zy, zz, 0, 0, 0, 0, 1));
}

export function setRotationQuaternion(tree, x, y, z, w) {
    tree.setRotationFromQuaternion(new THREE.Quaternion(x, y, z, w));
}

export function updateMatrix(tree) {
    tree.updateMatrix();
}

export function updateMatrixWorld(tree) {
    tree.updateMatrixWorld(true);
}

export function createLod() {
    var lod = new THREE.LOD();
    lod.name = "LOD";
    return lod;
}

export function addLod(lodTree, levelTree, distance) {
    levelTree.userData.distance = distance;
    lodTree.addLevel(levelTree, distance * 8);
}

export function createGeometry(vertData, normData, indData, uvData, computeNormals) {
    const verts = new Float32Array(vertData.length / 4);
    const vertDataView = new DataView(vertData.slice().buffer);
    for (let i = 0; i < vertData.length; i += 4) {
        verts[i / 4] = vertDataView.getFloat32(i, true);
    }

    const inds = new Int32Array(indData.length);
    indData.copyTo(inds);

    const geometry = new THREE.BufferGeometry();
    geometry.setIndex(new THREE.Uint32BufferAttribute(inds, 1));
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3));

    if (normData.length > 0 || !computeNormals) {
        const norms = new Float32Array(normData.length / 4);
        const normDataView = new DataView(normData.slice().buffer);
        for (let i = 0; i < normData.length; i += 4) {
            norms[i / 4] = normDataView.getFloat32(i, true);
        }

        geometry.setAttribute('normal', new THREE.Float32BufferAttribute(norms, 3));
    }

    const vertCount = verts.length / 3;
    const uvSetCount = uvData.length / 8 / vertCount;
    const uvBufferCount = uvData.length / uvSetCount;

    if (uvSetCount === 0) {
        geometry.setAttribute('uv', new THREE.Float32BufferAttribute(vertCount * 2, 2));
    }
    
    for (let i = 0; i < uvSetCount; i++) {
        const offset = i * uvBufferCount;
        const uvDataView = new DataView(uvData.slice(offset, offset + uvBufferCount).buffer);
        const uvs = new Float32Array(uvBufferCount / 4);
        for (let j = 0; j < uvBufferCount; j += 4) {
            uvs[j / 4] = uvDataView.getFloat32(j, true);
        }

        if (i === 0) {
            geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
        }
    }

    if (computeNormals) {
        geometry.computeVertexNormals();
    }

    //geometry.computeTangents();
    
    return geometry;
}

export function mergeGeometries(geometries) {
    return BufferGeometryUtils.mergeGeometries(geometries, true);
}

export function createInstancedMesh(geometry, materials, expectedMeshCount, receiveShadow, castShadow) {
    const mesh = new THREE.InstancedMesh(geometry, materials, expectedMeshCount);
    //mesh.perObjectFrustumCulled = false;
    //mesh.computeBVH({ margin: 0 });
    mesh.receiveShadow = receiveShadow;
    mesh.castShadow = castShadow;
    mesh.matrixAutoUpdate = false;
    mesh.matrixWorldAutoUpdate = false;
    return mesh;
}

export function createMesh(geometry, materials, receiveShadow, castShadow) {
    const mesh = new THREE.Mesh(geometry, materials);
    mesh.receiveShadow = receiveShadow;
    mesh.castShadow = castShadow;
    mesh.material.needsUpdate = true;
    return mesh;
}

export function getInstanceInfo(x, y, z, dir) {
    return {
        pos: { x, y, z },
        rotY: -dir * Math.PI / 2
    }
}

export function instantiate(tree, instanceInfos) {
    if (!tree.isInstancedMesh) {
        return;
    }

    /* for some reason, InstancedMesh2 is more expensive CPU-wise and GPU-wise than InstancedMesh, even with disabled frustum culling
    tree.addInstances(instanceInfos.length, (obj, index) => {
        let instanceInfo = instanceInfos[index];
        obj.position = new THREE.Vector3(instanceInfo.pos.x, instanceInfo.pos.y, instanceInfo.pos.z);
        obj.rotateY(instanceInfo.rotY);
    });*/
    
    for (let i = 0; i < instanceInfos.length; i++) {
        const instanceInfo = instanceInfos[i];
        const placementMatrix = new THREE.Matrix4();
        placementMatrix.makeRotationY(instanceInfo.rotY);
        placementMatrix.setPosition(instanceInfo.pos.x, instanceInfo.pos.y, instanceInfo.pos.z);
        tree.setMatrixAt(i, placementMatrix);
    }

    tree.instanceMatrix.needsUpdate = true;
}

export function getChildren(tree) {
    return tree.children;
}

export function getAllChildren(tree) {
    const children = tree.children;
    const allChildren = [];
    for (let i = 0; i < children.length; i++) {
        const child = children[i];
        allChildren.push(child);
        if (child.children.length > 0) {
            allChildren.push(...getAllChildren(child));
        }
    }
    return allChildren;
}

export function createVertexNormalHelper(mesh) {
    const helper = new VertexNormalsHelper(mesh, 1, 0x00ffff);
    helper.name = "helper";
    return helper;
}

export function createPointLight(r, g, b, intensity, distance, nightOnly) {
    const light = new THREE.PointLight(new THREE.Color(r, g, b), intensity, distance);
    light.userData.nightOnly = nightOnly;
    return light;
}

export function createSpotLight(parent, r, g, b, intensity, distance, angleInner, angleOuter, nightOnly) {

    const halfAngleRad = Math.min(THREE.MathUtils.degToRad(angleOuter / 2), Math.PI / 2);
    const penumbra = (angleOuter - angleInner) / angleOuter;
    // const decay = FalloffExponent;

    const light = new THREE.SpotLight(new THREE.Color(r, g, b), intensity * 100, distance /*, halfAngleRad, penumbra*/);
    light.castShadow = true;
    light.userData.nightOnly = nightOnly;
    light.visible = false; // something is still wrong about them

    const worldPos = new THREE.Vector3();
    parent.getWorldPosition(worldPos);

    const worldDir = new THREE.Vector3();
    parent.getWorldDirection(worldDir);

    const targetWorldPos = worldPos.clone().add(worldDir.multiplyScalar(distance));

    light.target.position.copy(targetWorldPos);
    light.target.updateMatrixWorld();

    return light;
}

export function createSpotLightHelper(spotLight) {
    return new THREE.SpotLightHelper(spotLight);
}

export function createPointLightHelper(pointLight) {
    return new THREE.PointLightHelper(pointLight);
}

export function createSphere(radius) {
    const geometry = new THREE.SphereGeometry(radius, 32, 16);
    return new THREE.Mesh(geometry, collisionMaterial);
}

export function createEllipsoid(radiusX, radiusY, radiusZ) {
    const geometry = new THREE.SphereGeometry(1, 32, 16);
    geometry.scale(radiusX, radiusY, radiusZ);
    return new THREE.Mesh(geometry, collisionMaterial);
}

export function createCollisionMesh(vertData, indData) {
    const verts = new Float32Array(vertData.length / 4);
    const vertDataView = new DataView(vertData.slice().buffer);
    for (let i = 0; i < vertData.length; i += 4) {
        verts[i / 4] = vertDataView.getFloat32(i, true);
    }

    const inds = new Int32Array(indData.length);
    indData.copyTo(inds);

    const geometry = new THREE.BufferGeometry();
    geometry.setIndex(new THREE.Uint32BufferAttribute(inds, 1));
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3));

    return new THREE.Mesh(geometry, collisionMaterial);
}

export function triangulate(positions3d) {
    return Earcut.triangulate(positions3d, null, 3);
}

export function log(tree) {
    //console.log(tree);
}