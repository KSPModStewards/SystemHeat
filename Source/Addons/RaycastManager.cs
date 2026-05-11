

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace SystemHeat.Addons;

[DefaultExecutionOrder(-1)]
[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
internal class RaycastManager : MonoBehaviour
{
  public static RaycastManager Instance { get; private set; }

  struct RaycastParams
  {
    public float range;
    public int layerMask;
  }

  const int DefaultCap = 128;

  private TransformAccessArray transforms;
  private NativeArray<RaycastParams> args;
  private NativeArray<RaycastHit> hits;
  private readonly Dictionary<int, int> mapping = [];
  private readonly Dictionary<int, int> reverse = [];
  private JobHandle handle = default;

  public void Register(PartModule module, Transform transform, float range, int layerMask = -5)
  {
    if (module == null || transform == null || this == null)
      return;

    handle.Complete();

    if (!enabled)
      enabled = true;

    var moduleID = module.GetInstanceID();
    var arg = new RaycastParams
    {
      range = range,
      layerMask = layerMask
    };

    // If the module is already registered then this overrides its existing
    // raycast request.
    if (mapping.TryGetValue(moduleID, out int index))
    {
      transforms[index] = transform;
      args[index] = arg;
      return;
    }

    EnsureCapacity();

    index = transforms.length;
    transforms.Add(transform);
    args[index] = arg;

    mapping.Add(moduleID, index);
    reverse.Add(index, moduleID);
  }

  public void Unregister(PartModule module)
  {
    if (module == null)
      return;

    handle.Complete();

    var moduleID = module.GetInstanceID();
    if (!mapping.TryGetValue(moduleID, out var index))
      return;

    mapping.Remove(moduleID);
    reverse.Remove(index);
    transforms.RemoveAtSwapBack(index);

    int last = transforms.length;
    if (reverse.TryGetValue(last, out var swappedID))
    {
      args[index] = args[last];
      if (hits.IsCreated && last < hits.Length)
        hits[index] = hits[last];

      mapping[swappedID] = index;
      reverse.Remove(last);
      reverse[index] = swappedID;
    }
  }

  /// <summary>
  /// Get the precomputed raycast hit for <paramref name="module"/>.
  /// </summary>
  /// <param name="module"></param>
  /// <returns><c>null</c> if there is no hit, and the hit otherwise.</returns>
  /// <remarks>
  /// <see cref="RaycastHit.collider" /> will be <c>null</c> if the raycast did
  /// not hit anything.
  /// </remarks>
  public RaycastHit? GetRaycastHit(PartModule module)
  {
    var moduleID = module.GetInstanceID();
    if (!mapping.TryGetValue(moduleID, out var index))
      return null;

    if ((uint)index >= (uint)hits.Length)
      return null;

    if (!handle.IsCompleted)
      handle.Complete();

    return hits[index];
  }

  void EnsureCapacity()
  {
    int length = transforms.length;
    if (length < transforms.capacity)
      return;

    var newcap = Math.Max(length * 2, 128);
    var nt = new TransformAccessArray(newcap, desiredJobCount: 1);
    var na = new NativeArray<RaycastParams>(newcap, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

    for (int i = 0; i < length; ++i)
      nt.Add(transforms[i]);
    NativeArray<RaycastParams>.Copy(args, na, length);

    transforms.Dispose();
    args.Dispose();

    transforms = nt;
    args = na;
  }

  void Awake()
  {
    Instance = this;
  }

  void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }

  void OnEnable()
  {
    transforms = new TransformAccessArray(DefaultCap, desiredJobCount: 1);
    args = new NativeArray<RaycastParams>(DefaultCap, Allocator.Persistent);
  }

  void OnDisable()
  {
    try
    {
      if (!handle.IsCompleted)
        handle.Complete();
    }
    catch (Exception e)
    {
      Debug.LogException(e);
    }

    transforms.Dispose();
    args.Dispose();
    if (hits.IsCreated)
      hits.Dispose();

    transforms = default;
    args = default;
    hits = default;

    mapping.Clear();
    reverse.Clear();
  }

  void FixedUpdate()
  {
    if (!handle.IsCompleted)
      handle.Complete();

    var count = mapping.Count;
    if (count == 0)
    {
      handle = default;
      return;
    }

    var commands = new NativeArray<RaycastCommand>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
    hits = new NativeArray<RaycastHit>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

    // RaycastCommand.ScheduleBatch reads maxHits immediately but the remaining
    // fields are not read until the job runs. So we need to initialize maxHits
    // immediately but the rest can happen in a job.
    for (int i = 0; i < count; ++i)
      commands[i] = new() { maxHits = 1 };

    handle = new BuildCommandJob { args = args, commands = commands }
      .Schedule(transforms, handle);
    handle = RaycastCommand.ScheduleBatch(commands, hits, 64, handle);
    JobHandle.ScheduleBatchedJobs();
  }

  void Update()
  {
    handle.Complete();

    if (mapping.Count == 0)
      enabled = false;
  }

  struct BuildCommandJob : IJobParallelForTransform
  {
    [ReadOnly]
    public NativeArray<RaycastParams> args;
    [WriteOnly]
    public NativeArray<RaycastCommand> commands;

    public void Execute(int index, TransformAccess transform)
    {
      var arg = args[index];
      var command = new RaycastCommand
      {
        from = transform.position,
        direction = transform.rotation * Vector3.forward,
        distance = arg.range,
        layerMask = arg.layerMask,
        maxHits = 1
      };

      commands[index] = command;
    }
  }
}