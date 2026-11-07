using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Components.Modules;

public partial class InputDisplay : ComponentBase
{
    public struct InputState
    {
        // State
        public float Accelerate;
        public float Brake;
        public float Steer;

        // Inputs
        public bool InputSteerLeft;
        public bool InputSteerRight;
        public float InputSteer;
        public bool InputAccelerate;
        public bool InputBrake;
        public float InputAccelerateReal;
        public float InputBrakeReal;
        public float InputGas;

        // Misc.
        public bool IsSteerReal;

        public bool IsAccelerateReal;
        public bool IsAccelerateGas;

        public bool IsBrakeReal;
        public bool IsBrakeGas;
    }

    private int currentInputIndex = -1;

    [Parameter, EditorRequired]
    public ImmutableArray<IInput>? OverrideInputs { get; set; }

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    public InputState CurrentInputState
    {
        get
        {
            if (currentInputIndex >= 0 && currentInputIndex < inputStates.Length)
                return inputStates[currentInputIndex];

            return new();
        }
    }

    public int CurrentInputIndex
    {
        get => currentInputIndex;
        set
        {
            currentInputIndex = value;
            StateHasChanged();
        }
    }

    private ImmutableArray<IInput> inputs;

    // Same source order as GhostControls, which provides CurrentInputIndex
    private ImmutableArray<IInput> Inputs => OverrideInputs ?? Ghost?.PlayerInputs?.FirstOrDefault()?.Inputs.ToImmutableArray() ?? Ghost?.Inputs ?? [];

    private ImmutableArray<InputState> inputStates = [];

    private ImmutableArray<InputState> GetInputStates(ImmutableArray<IInput> inputs)
    {
        var fakeIsRaceRunning = inputs.OfType<FakeIsRaceRunning>().FirstOrDefault();
        if(fakeIsRaceRunning.Time.TotalMilliseconds != 0)
        {
            inputs = inputs.Select(input => input.WithTime(input.Time - fakeIsRaceRunning.Time)).ToImmutableArray();
        }

        var inputStates = ImmutableArray.CreateBuilder<InputState>(inputs.Length);

        GBX.NET.GameVersion version = GBX.NET.GameVersion.MP4;
        if (Ghost is not null)
        {
            version = Ghost.GameVersion;
            if ((version & GBX.NET.GameVersion.TMF) != 0)
                version = GBX.NET.GameVersion.TMF;
            else if ((version & GBX.NET.GameVersion.MP1) != 0)
                version = GBX.NET.GameVersion.MP1;
            else if ((version & GBX.NET.GameVersion.MP2) != 0)
                version = GBX.NET.GameVersion.MP2;
            else if ((version & GBX.NET.GameVersion.MP3) != 0)
                version = GBX.NET.GameVersion.MP3;
            else if ((version & GBX.NET.GameVersion.TMT) != 0)
                version = GBX.NET.GameVersion.TMT;
            else if ((version & GBX.NET.GameVersion.MP4) != 0)
                version = GBX.NET.GameVersion.MP4;
            else
                version = GBX.NET.GameVersion.MP4; // Game not recognized, fall back to MP4.
        }

        GBX.NET.Inputs.FakeDontInverseAxis fakeDontInverseAxis = new(TmEssentials.TimeInt32.MinValue, false);
        GBX.NET.Inputs.Gas gas = new(TmEssentials.TimeInt32.MinValue, 0);
        GBX.NET.Inputs.Accelerate accelerate = new(TmEssentials.TimeInt32.MinValue, false);
        GBX.NET.Inputs.AccelerateReal accelerateReal = new(TmEssentials.TimeInt32.MinValue, 0);
        GBX.NET.Inputs.Brake brake = new(TmEssentials.TimeInt32.MinValue, false);
        GBX.NET.Inputs.BrakeReal brakeReal = new(TmEssentials.TimeInt32.MinValue, 0);
        IInputSteer steer = new GBX.NET.Inputs.Steer(TmEssentials.TimeInt32.MinValue, 0);
        GBX.NET.Inputs.SteerLeft steerLeft = new(TmEssentials.TimeInt32.MinValue, false);
        GBX.NET.Inputs.SteerRight steerRight = new(TmEssentials.TimeInt32.MinValue, false);

        InputState inputState = new();
        foreach(var input in inputs)
        {
            switch (input)
            {
                case GBX.NET.Inputs.FakeDontInverseAxis inputFakeDontInverseAxis:
                    fakeDontInverseAxis = inputFakeDontInverseAxis; break;
                case GBX.NET.Inputs.Gas inputGas:
                    gas = inputGas; break;
                case GBX.NET.Inputs.Accelerate inputAccelerate:
                    accelerate = inputAccelerate; break;
                case GBX.NET.Inputs.AccelerateReal inputAccelerateReal:
                    accelerateReal = inputAccelerateReal; break;
                case GBX.NET.Inputs.Brake inputBrake:
                    brake = inputBrake; break;
                case GBX.NET.Inputs.BrakeReal inputBrakeReal:
                    brakeReal = inputBrakeReal; break;
                case IInputSteer inputSteer: // Steer, SteerOld, SteerTM2020
                    steer = inputSteer; break;
                case GBX.NET.Inputs.SteerLeft inputSteerLeft:
                    steerLeft = inputSteerLeft; break;
                case GBX.NET.Inputs.SteerRight inputSteerRight:
                    steerRight = inputSteerRight; break;
            }


            #region Accelerate and Brake

            inputState.InputAccelerate = accelerate.Pressed;
            inputState.InputBrake = brake.Pressed;
            inputState.InputAccelerateReal = accelerateReal.NormalizedValue;
            inputState.InputBrakeReal = brakeReal.NormalizedValue;
            inputState.InputGas = gas.NormalizedValue;
            if (!fakeDontInverseAxis.Pressed
                && version == GBX.NET.GameVersion.TMT // FakeDontInverseAxis only applies to TMT (for acceleration/brake)
            )
                inputState.InputGas *= -1f;

            inputState.Accelerate = 0f;
            inputState.Brake = 0f;

            inputState.IsAccelerateReal = false;
            inputState.IsAccelerateGas = false;
            inputState.IsBrakeReal = false;
            inputState.IsBrakeGas = false;

            switch (version)
            {
            case GBX.NET.GameVersion.TMF:
            case GBX.NET.GameVersion.MP1:
            {
                int latestABDigitalTime = Math.Max(accelerate.Time.TotalMilliseconds, brake.Time.TotalMilliseconds);
                int gasTime = gas.Time.TotalMilliseconds;

                if (latestABDigitalTime == gasTime && !accelerate.Pressed && !brake.Pressed && 0.01f < Math.Abs(gas.NormalizedValue))
                    gasTime++;

                inputState.Accelerate = 1f;
                if(gasTime <= latestABDigitalTime)
                {
                    if(!accelerate.Pressed)
                        inputState.Accelerate = 0f;
                    if(brake.Pressed)
                        inputState.Brake = 1f;
                }
                else
                {
                    inputState.IsAccelerateGas = true;
                    inputState.IsBrakeGas = true;

                    if (gas.NormalizedValue <= 0.3f)
                    {
                        inputState.Accelerate = 0f;

                        if(gas.NormalizedValue <= -0.3f)
                        {
                            inputState.Accelerate = 0f;
                            inputState.Brake = 1f;
                        }
                    }
                    else
                        inputState.Accelerate = 1f;
                }

                break;
            }
            case GBX.NET.GameVersion.MP3:
            case GBX.NET.GameVersion.TMT:
            {
                int latestABRealTime = Math.Max(accelerateReal.Time.TotalMilliseconds, brakeReal.Time.TotalMilliseconds);
                int latestABDigitalTime = Math.Max(accelerate.Time.TotalMilliseconds, brake.Time.TotalMilliseconds);

                if ((latestABRealTime == latestABDigitalTime) && !accelerate.Pressed && !brake.Pressed && (0.01f < accelerateReal.NormalizedValue || 0.01f < brakeReal.NormalizedValue))
                    latestABRealTime++;

                bool isGas = latestABRealTime < gas.Time.TotalMilliseconds
                    && (version == GBX.NET.GameVersion.TMT || latestABDigitalTime < gas.Time.TotalMilliseconds);  // TMT does not check for digital inputs here

                if (isGas)
                {
                    inputState.IsAccelerateGas = true;
                    inputState.IsBrakeGas = true;

                    if (-0.3f < gas.NormalizedValue)
                    {
                        if (0.3f <= gas.NormalizedValue)
                        {
                            if (!fakeDontInverseAxis.Pressed
                                && version == GBX.NET.GameVersion.TMT // FakeDontInverseAxis only applies to TMT (for acceleration/brake)
                            )
                                inputState.Brake = 1f;
                            else
                                inputState.Accelerate = 1f;
                        }
                    }
                    else if (!fakeDontInverseAxis.Pressed
                        && version == GBX.NET.GameVersion.TMT // FakeDontInverseAxis only applies to TMT (for acceleration/brake)
                    )
                        inputState.Accelerate = 1f;
                    else
                        inputState.Brake = 1f;
                }

                if (!isGas
                    || version == GBX.NET.GameVersion.TMT   // TMT does this no matter if Gas was applied or not
                )
                {
                    if(latestABDigitalTime < latestABRealTime)
                    {
                        if (0.3f <= accelerateReal.NormalizedValue)
                        {
                            inputState.IsAccelerateReal = true;
                            inputState.IsAccelerateGas = false;

                            inputState.Accelerate = 1f;
                        }
                        if (0.3f <= brakeReal.NormalizedValue)
                        {
                            inputState.IsBrakeReal = true;
                            inputState.IsBrakeGas = false;

                            inputState.Brake = 1f;
                        }
                    }
                    else
                    {
                        if (accelerate.Pressed)
                        {
                            inputState.IsAccelerateReal = false;
                            inputState.IsAccelerateGas = false;

                            inputState.Accelerate = 1f;
                        }
                        if (brake.Pressed)
                        {
                            inputState.IsBrakeReal = false;
                            inputState.IsBrakeGas = false;

                            inputState.Brake = 1f;
                        }
                    }
                }

                break;
            }
            case GBX.NET.GameVersion.MP4:
            {
                int accelerateRealTime = accelerateReal.Time.TotalMilliseconds;
                float accelerateRealValue = accelerateReal.NormalizedValue;

                inputState.IsAccelerateReal = true;
                if (0.01f < gas.NormalizedValue && accelerateRealTime < gas.Time.TotalMilliseconds)
                {
                    inputState.IsAccelerateReal = false;
                    inputState.IsAccelerateGas = true;

                    accelerateRealTime = gas.Time.TotalMilliseconds;
                    accelerateRealValue = gas.NormalizedValue;
                }

                if (accelerateRealTime == accelerate.Time.TotalMilliseconds && !accelerate.Pressed && 0.01f < accelerateReal.NormalizedValue)
                    accelerateRealTime++;

                if (accelerate.Time.TotalMilliseconds < accelerateRealTime ? 0.3f <= accelerateRealValue : accelerate.Pressed)
                    inputState.Accelerate = 1f;
                
                if (accelerate.Time.TotalMilliseconds >= accelerateRealTime)
                {
                    inputState.IsAccelerateReal = false;
                    inputState.IsAccelerateGas = false;
                }


                int brakeRealTime = brakeReal.Time.TotalMilliseconds;
                float brakeRealValue = brakeReal.NormalizedValue;

                inputState.IsBrakeReal = true;
                if (gas.NormalizedValue < -0.01 && brakeRealTime < gas.Time.TotalMilliseconds)
                {
                    inputState.IsBrakeReal = false;
                    inputState.IsBrakeGas = true;

                    brakeRealTime = gas.Time.TotalMilliseconds;
                    brakeRealValue = -gas.NormalizedValue;
                }

                if (brakeRealTime == brake.Time.TotalMilliseconds && !brake.Pressed && 0.01f < brakeReal.NormalizedValue)
                    brakeRealTime++;

                if (brake.Time.TotalMilliseconds < brakeRealTime ? 0.3f <= brakeRealValue : brake.Pressed)
                    inputState.Brake = 1f;

                if (brake.Time.TotalMilliseconds >= brakeRealTime)
                {
                    inputState.IsBrakeReal = false;
                    inputState.IsBrakeGas = false;
                }

                break;
            }
            }

            #endregion

            #region Steering

            inputState.InputSteerLeft = steerLeft.Pressed;
            inputState.InputSteerRight = steerRight.Pressed;
            inputState.InputSteer = steer.NormalizedValue;
            if (fakeDontInverseAxis.Pressed)
                inputState.InputSteer *= -1f;

            int latestSteerRealTime = steer.Time.TotalMilliseconds;
            int latestSteerDigitalTime = Math.Max(steerLeft.Time.TotalMilliseconds, steerRight.Time.TotalMilliseconds);

            if (latestSteerDigitalTime == steer.Time.TotalMilliseconds)
            {
                if (!steerLeft.Pressed && !steerRight.Pressed && 0.01f < Math.Abs(steer.NormalizedValue))
                    latestSteerRealTime++;
            }

            if (latestSteerRealTime <= latestSteerDigitalTime)
            {
                inputState.IsSteerReal = false;

                if (!steerLeft.Pressed)
                {
                    if (steerRight.Pressed)
                        inputState.Steer = 1f;
                    else
                        inputState.Steer = 0f;
                }
                else if (!steerRight.Pressed
                    || version == GBX.NET.GameVersion.TMF   // In TMF, SteerLeft always overpowers SteerRight
                    || steerRight.Time.TotalMilliseconds <= steerLeft.Time.TotalMilliseconds
                )
                    inputState.Steer = -1f;
                else
                    inputState.Steer = 1f;
            }
            else
            {
                inputState.IsSteerReal = true;

                inputState.Steer = -steer.NormalizedValue;

                if (!fakeDontInverseAxis.Pressed)
                    inputState.Steer = -inputState.Steer;
            }

            #endregion

            inputStates.Add(inputState);
        }

        return inputStates.MoveToImmutable();
    }

    protected override void OnParametersSet()
    {
        var newInputs = Inputs;

        if (newInputs != inputs)
        {
            inputs = newInputs;
            inputStates = GetInputStates(inputs);
        }
    }
}
