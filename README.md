# ARETT: Augmented Reality Eye Tracking Toolkit for Head Mounted Displays

This is a toolkit for Unity 3D which contains scripts for acquiring eye tracking data on the Microsoft HoloLens 2.

## Documentation

Documentation about the usage of the toolkit can be found in the [projects wiki](https://github.com/AR-Eye-Tracking-Toolkit/ARETT/wiki). Background information is provided in the [associated publication](#reference).

## Analysis package

An accompaning package for analysis of the acquired data using R can be found in the [ARETT R Package](https://github.com/AR-Eye-Tracking-Toolkit/ARETT-R-Package) repository.

## Demo Application

A sample application integrating the toolkit into Unity project can be found in the [ARETT Demo](https://github.com/AR-Eye-Tracking-Toolkit/ARETT-Demo) repository.

## Dependencies

The toolkit is currently being developed for [Unity](https://unity.com/releases/2019-lts) [2019.4.20f1](https://unity3d.com/unity/whats-new/2019.4.20) and the [Microsoft Mixed Reality Toolkit](https://github.com/microsoft/MixedRealityToolkit-Unity) [v2.5.4](https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/v2.5.4). However, it should work with all releases of the Unity 2019.4 LTS branch and future releases of Mixed Reality Toolkit. Compatibility with future versions of Unity is likely but not guranteed.

### Unity 2020

As the toolkit was originally developed for Unity 2019, compatibility with Unity 2020 is not guranteed. If no gaze data is recorded and the recordings are empty, the change by wangsk described in [issue #4](https://github.com/AR-Eye-Tracking-Toolkit/ARETT/issues/4#issuecomment-1068179051) might fix the issue.

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
