//
// Simple pooling for Unity.
//   Author: Martin "quill18" Glaude (quill18@quill18.com)
//   Latest Version: https://gist.github.com/quill18/5a7cfffae68892621267
//   License: CC0 (http://creativecommons.org/publicdomain/zero/1.0/)
//   UPDATES:
// 	2015-04-16: Changed Pool to use a Stack generic.
// 
// Usage:
// 
//   There's no need to do any special setup of any kind.
// 
//   Instead of calling Instantiate(), use this:
//       SimplePool.Spawn(somePrefab, somePosition, someRotation);
// 
//   Instead of destroying an object, use this:
//       SimplePool.Despawn(myGameObject);
// 
//   If desired, you can preload the pool with a number of instances:
//       SimplePool.Preload(somePrefab, 20);
// 
// Remember that Awake and Start will only ever be called on the first instantiation
// and that member variables won't be reset automatically.  You should reset your
// object yourself after calling Spawn().  (i.e. You'll have to do things like set
// the object's HPs to max, reset animation states, etc...)
// 
// 
// 


using UnityEngine;
using System.Collections.Generic;

public class SimplePoolDebugger: MonoBehaviour{
    public bool updateInfo;

    [System.Serializable]
    public class PoolDesc{
        public string name;
        public GameObject go;
        public int inactiveCount;
        public int  nextID;
    }

    public List<PoolDesc> poolDescs;
    static public SimplePoolDebugger inst;

    static public void UpdateNow(){
        inst.UpdateInfo();
    }

    private void Awake() {
        inst = this;
    }

    void Update() {
        if (updateInfo) {
            updateInfo = false;
            UpdateInfo();
        }
    }

    public void UpdateInfo() {
        //Debug.Log("UpdateInfo() A:", this);
        if (SimplePool.pools != null) {
            //Debug.Log("UpdateInfo() B:", this);
            foreach (var kvp in SimplePool.pools) {
                var pd = poolDescs.Find((obj) => obj.go == kvp.Key);
                if (pd == null) {
                    pd = new PoolDesc();
                    poolDescs.Add(pd);
                }
                pd.go = kvp.Key;
                pd.inactiveCount = kvp.Value.inactive.Count;
                pd.nextID = kvp.Value.nextId;
                pd.name = pd.go.name;

            }
        } 
    }
}


public static class SimplePool {

	// You can avoid resizing of the Stack's internal data by
	// setting this to a number equal to or greater to what you
	// expect most of your pool sizes to be.
	// Note, you can also use Preload() to set the initial size
	// of a pool -- this can be handy if only some of your pools
	// are going to be exceptionally large (for example, your bullets.)
	const int DEFAULT_POOL_SIZE = 3;

	/// <summary>
	/// The Pool class represents the pool for a particular prefab.
	/// </summary>
	public class Pool {
		// We append an id to the name of anything we instantiate.
		// This is purely cosmetic.
        //Also used to estimate initial size of pool including active
		public int nextId=0;

		// The structure containing our inactive objects.
		// Why a Stack and not a List? Because we'll never need to
		// pluck an object from the start or middle of the array.
		// We'll always just grab the last one, which eliminates
		// any need to shuffle the objects around in memory.
		public Stack<GameObject> inactive;

		// The prefab that we are pooling
		GameObject prefab;

		// Constructor
		public Pool(GameObject prefab, int initialQty) {
			this.prefab = prefab;

			// If Stack uses a linked list internally, then this
			// whole initialQty thing is a placebo that we could
			// strip out for more minimal code. But it can't *hurt*.
			inactive = new Stack<GameObject>(initialQty);
            //Debug.Log("Pool: " + prefab.name + " : "+ initialQty);
		}

        public void Empty(){
            while(inactive.Count>0){
                var obj = inactive.Pop();
                GameObject.Destroy(obj);
            }
            nextId = 0;
        }

		// Spawn an object from our pool
		public GameObject Spawn(Vector3 pos, Quaternion rot) {
			GameObject obj;
			if(inactive.Count==0) {
				// We don't have an object in our pool, so we
				// instantiate a whole new object.
				obj = (GameObject)GameObject.Instantiate(prefab, pos, rot);
				obj.name = prefab.name + " ("+(nextId++)+")";

				// Add a PoolMember component so we know what pool
				// we belong to.
				obj.AddComponent<PoolMember>().myPool = this;
			}
			else {
				// Grab the last object in the inactive array
				obj = inactive.Pop();

				if(obj == null) {
					// The inactive object we expected to find no longer exists.
					// The most likely causes are:
					//   - Someone calling Destroy() on our object
					//   - A scene change (which will destroy all our objects).
					//     NOTE: This could be prevented with a DontDestroyOnLoad
					//	   if you really don't want this.
					// No worries -- we'll just try the next one in our sequence.

					return Spawn(pos, rot);
				}
			}

			obj.transform.position = pos;
			obj.transform.rotation = rot;
			obj.SetActive(true);
			return obj;

		}

		// Return an object to the inactive pool.
		public void Despawn(GameObject obj) {
			obj.SetActive(false);

			// Since Stack doesn't have a Capacity member, we can't control
			// the growth factor if it does have to expand an internal array.
			// On the other hand, it might simply be using a linked list 
			// internally.  But then, why does it allow us to specify a size
			// in the constructor? Maybe it's a placebo? Stack is weird.
			inactive.Push(obj);
            //Debug.Log("Despawn Pool: " + prefab.name +" : "+ inactive.Count);
		}

	}


	/// <summary>
	/// Added to freshly instantiated objects, so we can link back
	/// to the correct pool on despawn.
	/// </summary>
	class PoolMember : MonoBehaviour {
		public Pool myPool;
	}

	// All of our pools
	static public Dictionary< GameObject, Pool > pools;

	/// <summary>
	/// Initialize our dictionary.
	/// </summary>
	static void Init (GameObject prefab=null, int qty = DEFAULT_POOL_SIZE) {
		if(pools == null) {
			pools = new Dictionary<GameObject, Pool>();
		}
		if(prefab!=null && pools.ContainsKey(prefab) == false) {
			pools[prefab] = new Pool(prefab, qty);
		}
	}

	/// <summary>
	/// If you want to preload a few copies of an object at the start
	/// of a scene, you can use this. Really not needed unless you're
	/// going to go from zero instances to 100+ very quickly.
	/// Could technically be optimized more, but in practice the
	/// Spawn/Despawn sequence is going to be pretty darn quick and
	/// this avoids code duplication.
	/// </summary>
	static public void Preload(GameObject prefab, int qty = 1) {
        //Debug.Log("Preload: " + prefab.name);
		Init(prefab, qty);

		// Make an array to grab the objects we're about to pre-spawn.
		GameObject[] obs = new GameObject[qty];
		for (int i = 0; i < qty; i++) {
			obs[i] = Spawn (prefab, Vector3.zero, Quaternion.identity);
		}

		// Now despawn them all.
		for (int i = 0; i < qty; i++) {
			Despawn( obs[i] );
		}
	}

    static public void PreloadTo(GameObject prefab, int qty) {
        qty -= PoolInactiveSize(prefab);
        if (qty < 1) return;
        Preload(prefab, qty);

    }


	/// <summary>
	/// Spawns a copy of the specified prefab (instantiating one if required).
	/// NOTE: Remember that Awake() or Start() will only run on the very first
	/// spawn and that member variables won't get reset.  OnEnable will run
	/// after spawning -- but remember that toggling IsActive will also
	/// call that function.
	/// </summary>
	static public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot) {
        Init(prefab);

		return pools[prefab].Spawn(pos, rot);
		
	}

	/// <summary>
	/// Despawn the specified gameobject back into its pool.
	/// </summary>
	static public void Despawn(GameObject obj) {
		PoolMember pm = obj.GetComponent<PoolMember>();
        if(pm == null || pm.myPool == null) {
			//Debug.Log ("Object '"+obj.name+"' wasn't spawned from a pool. Destroying it instead.");
			GameObject.Destroy(obj);
		}
		else {
			pm.myPool.Despawn(obj);
		}
	}

    static public void ClearAll(){
        if (pools == null) return;
        foreach(var val in pools.Keys){
            EmptyPool(val);
        }
        pools.Clear();
    }

    static public void EmptyPool(GameObject prefab) {
        if (pools == null) return;
        if(pools.ContainsKey(prefab)){
            //Debug.Log("EmptyPool B: " + prefab);
            pools[prefab].Empty(); 
        }
    }

    static public int PoolSize(GameObject prefab) {
        if (pools == null) return 0;
        if (pools.ContainsKey(prefab)) {
            return pools[prefab].nextId;
        }
        return 0;
    }

    static public int PoolInactiveSize(GameObject prefab) {
        if (pools == null) return 0;
        if (pools.ContainsKey(prefab)) {
            return pools[prefab].inactive.Count;
        }
        return 0;
    }

    static public void IncreasePool(float ratio){
        foreach(var p in pools){
            int incr = Mathf.FloorToInt(p.Value.nextId * ratio);
            if(incr>0){
                Preload(p.Key, incr);
            }
        } 
    }
}