import * as THREE from 'three';

const textureLoader = new THREE.TextureLoader();

export function createRandomMaterial() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}

export function createMaterial(diffuseTexture, normalTexture, specularTexture, doubleSided, worldUV, transparent, blend) {
    const material = new THREE.MeshStandardMaterial({
        map: diffuseTexture,
        normalMap: normalTexture,
        //specularMap: specularTexture,
        transparent: transparent,
        side: doubleSided ? THREE.DoubleSide : THREE.FrontSide
    });

    if (worldUV && material.map) {
        material.onBeforeCompile = (shader) => {
            shader.uniforms.uvTransform = { value: new THREE.Matrix3().set(1 / 16, 0, 0, 0, 1 / 16, 0, 0, 0, 1) };

            shader.vertexShader = `
                uniform mat3 uvTransform;
          
                ${shader.vertexShader}`.replace('#include <uv_vertex>\n', '')
                .replace(
                    '#include <worldpos_vertex>',
                    `vec4 worldPosition = vec4(transformed, 1.0);
                    #ifdef USE_INSTANCING
                    worldPosition = instanceMatrix * worldPosition;
                    #endif
                    worldPosition = modelMatrix * worldPosition;
                    vMapUv = (uvTransform * vec3(worldPosition.xz, 1)).xy;`
            );
        };
    }

    return material;
}

export function createTexture(path) {
    const texture = textureLoader.load(path);
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    return texture;
}