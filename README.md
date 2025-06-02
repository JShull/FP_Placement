# FuzzPhyte Unity Tools

## XR

FP_XR is designed and built to be utilized with FP_Utility and FP_Ray with the intention of having some core functions that could be helpful in XR projects with 0 dependencies to other XR libraries. Includes a robust set of Controller Event scripts that can be wired up to whatever XR/VR package you're using and with a few lines of code can heavily modify button overlay text/icon information as well as lock the controller. The internal system requires some setup mainly tied to the delegates/events associated with at the controller(s) level or the button(s) level of each controller.

## Setup & Design

This package could be used in non-xr projects as a lot of the features/functions are more about spatial layout than anything else.

SamplesURP will require additional package imports.

* com.unity.render-pipelines.universal

### Software Architecture

Stacked components for interactive content like labels moving and labels typing. On the controller interaction side, heavy use of delegates/events that you can subscribe to for when those events fire off. Will require some intervention "wiring" up to the various XR/VR libraries you're using. At this level this just manages the setup and use case across button state changes and controller information. It lets you build out how you want your controller overlay UI information to be presented and all runs via scriptable objects. Just fire up a new one and reset the controller and the entire interface resets. No need to resubscribe to the events - you've already subscribed! - just need to point the system to the new data file and tell it to take action.

### Ways to Extend

## Dependencies

Please see the [package.json](./package.json) file for more information.

## License Notes

* This software running a dual license
* Most of the work this repository holds is driven by the development process from the team over at Unity3D :heart: to their never ending work on providing fantastic documentation and tutorials that have allowed this to be born into the world.
* I personally feel that software and it's practices should be out in the public domain as often as possible, I also strongly feel that the capitalization of people's free contribution shouldn't be taken advantage of.
  * If you want to use this software to generate a profit for you/business I feel that you should equally 'pay up' and in that theory I support strong copyleft licenses.
  * If you feel that you cannot adhere to the GPLv3 as a business/profit please reach out to me directly as I am willing to listen to your needs and there are other options in how licenses can be drafted for specific use cases, be warned: you probably won't like them :rocket:

### Educational and Research Use MIT Creative Commons

* If you are using this at a Non-Profit and/or are you yourself an educator and want to use this for your classes and for all student use please adhere to the MIT Creative Commons License
* If you are using this back at a research institution for personal research and/or funded research please adhere to the MIT Creative Commons License
  * If the funding line is affiliated with an [SBIR](https://www.sbir.gov) be aware that when/if you transfer this work to a small business that work will have to be moved under the secondary license as mentioned below.

### Commercial and Business Use GPLv3 License

* For commercial/business use please adhere by the GPLv3 License
* Even if you are giving the product away and there is no financial exchange you still must adhere to the GPLv3 License

## Contact

* [John Shull](mailto:JShull@fuzzphyte.com)

### Additional Notes
