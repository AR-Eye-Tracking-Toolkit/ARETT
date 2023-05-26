# ARETT: Augmented Reality Eye Tracking Toolkit for Head Mounted Displays

This is a toolkit for Unity 3D which contains scripts for acquiring eye tracking data on the Microsoft HoloLens 2.

## Documentation

Documentation about the usage of the toolkit can be found in the [projects wiki](https://github.com/AR-Eye-Tracking-Toolkit/ARETT/wiki). Background information is provided in the [associated publication](#reference).

## Analysis package

An accompaning package for analysis of the acquired data using R can be found in the [ARETT R Package](https://github.com/AR-Eye-Tracking-Toolkit/ARETT-R-Package) repository.

## Demo Application

A sample application integrating the toolkit into Unity project can be found in the [ARETT Demo](https://github.com/AR-Eye-Tracking-Toolkit/ARETT-Demo) repository.

## Dependencies

The toolkit is currently being developed for [Unity](https://unity.com/releases/2021-lts) [2021.3.2f1](https://unity3d.com/unity/whats-new/2021.3.2) and the [Microsoft Mixed Reality Toolkit](https://github.com/microsoft/MixedRealityToolkit-Unity) [v2.8.3](https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/v2.8.3). However, it should work with all releases of the Unity 2021.3 LTS branch and future releases of Mixed Reality Toolkit. Compatibility with future versions of Unity is likely but not guranteed.

### Unity 2019

The latest Unity 2019-compatible version is [ARETT v1.1.0](https://github.com/AR-Eye-Tracking-Toolkit/ARETT/releases/tag/v1.1.0). This is also the original version documented in the publication.

## Reference

Article: https://www.mdpi.com/1424-8220/21/6/2234

```tex
@article{Kapp.2021,
    title={ARETT: Augmented Reality Eye Tracking Toolkit for Head Mounted Displays},
    author={Kapp, Sebastian and Barz, Michael and Mukhametov, Sergey and Sonntag, Daniel and Kuhn, Jochen},
    journal={Sensors},
    volume={21},
    number={6},
    pages={2234},
    ISSN={1424-8220},
    url={http://dx.doi.org/10.3390/s21062234},
    DOI={10.3390/s21062234},
    publisher={MDPI AG},
    month={Mar},
    year={2021}
}
```
