import * as THREE from 'three';

const textureLoader = new THREE.TextureLoader();

export function createRandomMaterial() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}

export function createInvisibleMaterial() {
    return new THREE.MeshBasicMaterial({ visible: false });
}

export function createMaterial(
    diffuseTexture,
    normalTexture,
    specularTexture,
    blend2Texture,
    blendIntensityTexture,
    blend3Texture,
    doubleSided,
    worldUV,
    transparent,
    basic,
    specularAlpha,
    opacity,
    add,
    nightOnly,
    invisible) {
    let material;
    if (basic) {
        material = new THREE.MeshBasicMaterial({
            map: diffuseTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide
        });
    }
    else if (specularAlpha || specularTexture)
    {
        material = new THREE.MeshPhongMaterial({
            map: diffuseTexture,
            normalMap: normalTexture,
            specularMap: specularTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide,
        });
    }
    else {
        material = new THREE.MeshStandardMaterial({
            map: diffuseTexture,
            normalMap: normalTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide,
            blending: add ? THREE.AdditiveBlending : THREE.NormalBlending,
            depthWrite: !add, // some stadium transparency sorting problems
            visible: !invisible,
        });
    }

    if (specularAlpha) {
        material.onBeforeCompile = (shader) => {
            // set specular based on diffuse alpha, not sure if this does something yet
            shader.fragmentShader.replace(
                '#include <specularmap_fragment>',
                `
                #include <specularmap_fragment>
                specularStrength = sampledDiffuseColor.a;
            `
            )
        };
    }
    else if (opacity != 0) {
        material.onBeforeCompile = (shader) => {
            shader.uniforms.customOpacity = { value: opacity };

            shader.fragmentShader = `
                uniform float customOpacity;

                ${shader.fragmentShader}`.replace(
                '#include <map_fragment>',
                `
                    #include <map_fragment>
                    diffuseColor.a = customOpacity;
                `
            );
        };
    }
    else if ((worldUV && material.map) || (blend2Texture && blendIntensityTexture)) {
        material.onBeforeCompile = (shader) => {
            if (worldUV && material.map) {
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
            }

            if (blend2Texture && blendIntensityTexture) {
                shader.uniforms.blend2Texture = { value: blend2Texture };
                shader.uniforms.blendIntensityTexture = { value: blendIntensityTexture };
                shader.uniforms.blend3Texture = { value: blend3Texture };
                shader.fragmentShader = `
                uniform sampler2D blend2Texture;
                uniform sampler2D blendIntensityTexture;
                uniform sampler2D blend3Texture;
                ${shader.fragmentShader}`.replace('#include <map_fragment>', `
                #include <map_fragment>
                vec4 blend2Color = texture2D(blend2Texture, vMapUv);
                float blendIntensity = texture2D(blendIntensityTexture, vMapUv).r;
                vec4 blend3Color = texture2D(blend3Texture, vMapUv);
                diffuseColor = mix(mix(diffuseColor, blend2Color, blendIntensity), blend3Color, blend3Color.a);
                `);
            }
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