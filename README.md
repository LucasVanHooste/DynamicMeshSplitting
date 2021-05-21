# Dynamic Mesh Splitting

## Description
Dynamic Mesh Splitting is a Unity package for splitting 3D meshes at runtime. The algorithm uses an infinite plane in 3D space to divide a mesh into two new meshes. Please see the Capabilities & Limitations section for more information about what the algorithm can do.

## Installation

Find the latest DynamicMeshSplitting **Unity package** in the **Releases** and import it into your project.

## Contents
The Unity package contains a Scripts folder whichs holds all the splittng code, as well as a Demo folder in which you can find a DemoScene to try the splitting out.

## Usage

There are two recommended ways of using this package:
1. MeshSplitter/Splittable workflow. (simplified, creates objects for you)
2. MeshSplitter only workflow. (custom object creation)

### 1. MeshSplitter/Splittable workflow

To split an object using this workflow, add a **Splittable component** to the GameObject you want to split and fill in its inspector fields. For skinned meshes, add a **SplittableSkinned component**.

All you have to do now is reference the component in code and call one of two methods:

```cs
Splittable splittable;
PointPlane plane = new PointPlane(somePosition, someRotation);

SplitResult s = splittable.Split(plane);
//OR
splittable.SplitAsync(plane, SomeCallback);

private void SomeCallback(SplitResult s) {
   ///
}
```
It is recommended to use **Splittable.SplitAsync(...)** since this does the splitting computations on a different thread. 
* Split() returns a SplitResult struct containing the resulting GameObjects from the splitting. 
* SplitAsync() does not return anything but instead takes a second argument of type Action<SplitResult>, which is called on the main thread after computations have finished.

*Inside the Splittable.Split(Async)(...) methods, a MeshSplitter object is created which computates the splitting. When the splitting is computed, new objects are created for you using the result of the splitting computation.*

### 2. MeshSplitter only workflow

This workflow allows you to take the result of a splitting computation and use it to create your own custom objects.

To split a mesh using this workflow, create a **MeshSplitter** instance and  call SplitMesh() or SplitMeshAsync() on it to compute the split result. 

To split a skinned mesh, create a **MeshSplitterSkinned** instance and call SplitSkinnedMesh() or SplitSkinnedMeshAsync(). These metods takes a SkinnedMeshrenderer as first argument.

```cs
MeshSplitter meshSplitter = new MeshSplitter();

MeshSplitData d = meshSplitter.SplitMesh(targetMeshFilter, plane);
//OR
meshSplitter.SplitMeshAsync(targetMeshFilter, plane, SomeCallback);

private void SomeCallback(MeshSplitData d) {
   ///
}
```
It is recommended to use **MeshSplitter.SplitMeshAsync()** since this does the splitting computations on a different thread.

* SplitMesh() takes a MeshFilter component as its first argument, of which it uses the sharedMesh and transform. The second argument is the plane that splits the mesh. The function returns a MeshSplitData struct containing two instances of MeshData (one for each side of the plane). MeshData can be used to construct a new mesh.

* SplitMeshAsync() does not return anything but instead takes a third argument of type Action<MeshSplitData>, which is called on the main thread after computations have finished.

If you want to split a skinned mesh. Create an instance of MeshSplitterSkinned instead. MeshSplitterSkinned .SplitSkinnedMesh() takes a SkinnedMeshrenderer as first argument.

## Capabilities & Limitations

* This algorithm is able to split concave meshes that don't have holes which are created by the shape of their geomerty. This means it won't be able to split torus shapes correctly across.

* Splitting of skinned meshes is supported

* Meshes with submeshes are also supported.

* Splitting can be done asynchronous to avoild lag spikes.

* Custom materials and uv bounds can be applied to the caps created by splitting.

* Island detection is not implemented, meaning that all parts of a mesh that lie on the same side of the plane are part of the same split mesh. Even if they are not attached to eachother anymore.

## Supported Unity Versions
Unity versions **2019.4.6 and newer** are supported. The package and demo were created in version **2019.4.6** so any older versions are not officially supported. The splitting scripts will likely still work in older versions, but this has not been tested.

## Recommendations
* Use **IL2CPP** scripting backend for increased performance.
* Check out the demo scene to see the splitting in action.

## Contributing
I am currently not accepting pull requests but feel free to create an issue if you find any bugs.

## Donations
Unity Mesh Splitting is completely free, but if you would like to show your support feel free to donate at:
[https://www.paypal.me/LucasVanHooste](https://www.paypal.me/LucasVanHooste)

## License
Dynamic Mesh Splitting is licenced under [MIT](https://choosealicense.com/licenses/mit/)