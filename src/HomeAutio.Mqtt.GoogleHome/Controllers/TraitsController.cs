﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HomeAutio.Mqtt.GoogleHome.ActionFilters;
using HomeAutio.Mqtt.GoogleHome.Models;
using HomeAutio.Mqtt.GoogleHome.Models.State;
using HomeAutio.Mqtt.GoogleHome.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HomeAutio.Mqtt.GoogleHome.Controllers
{
    /// <summary>
    /// Traits controller.
    /// </summary>
    [Authorize]
    public class TraitsController : Controller
    {
        private readonly ILogger<TraitsController> _log;

        private readonly GoogleDeviceRepository _deviceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraitsController"/> class.
        /// </summary>
        /// <param name="logger">Logging instance.</param>
        /// <param name="deviceRepository">Device repository.</param>
        public TraitsController(
            ILogger<TraitsController> logger,
            GoogleDeviceRepository deviceRepository)
        {
            _log = logger;
            _deviceRepository = deviceRepository;
        }

        /// <summary>
        /// Edit device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <returns>Response.</returns>
        [ImportModelState]
        public IActionResult Create([Required] string deviceId)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var model = new TraitViewModel();

            return View(model);
        }

        /// <summary>
        /// Edit trait.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="viewModel">View Model.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        [ExportModelState]
        public IActionResult Create([Required] string deviceId, TraitViewModel viewModel)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var device = _deviceRepository.Get(deviceId);
            if (device.Traits.Any(x => x.Trait == viewModel.Trait))
                ModelState.AddModelError("Trait", "Device already contains trait");

            if (!ModelState.IsValid)
                return RedirectToAction("Create", new { deviceId });

            // Set values
            var trait = new DeviceTrait
            {
                Trait = viewModel.Trait,
                Attributes = !string.IsNullOrEmpty(viewModel.Attributes) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(viewModel.Attributes, new ObjectDictionaryConverter()) : null,
                Commands = !string.IsNullOrEmpty(viewModel.Commands) ? JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(viewModel.Commands) : null,
                State = !string.IsNullOrEmpty(viewModel.State) ? JsonConvert.DeserializeObject<Dictionary<string, DeviceState>>(viewModel.State) : null
            };

            device.Traits.Add(trait);

            // Save changes
            _deviceRepository.Persist();

            return RedirectToAction("Edit", "Devices", new { deviceId });
        }

        /// <summary>
        /// Delete device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="traitId">Trait id.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        public IActionResult Delete([Required] string deviceId, [Required] string traitId)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var device = _deviceRepository.Get(deviceId);

            var traitEnumId = traitId.ToEnum<TraitType>();
            if (!device.Traits.Any(x => x.Trait == traitEnumId))
                return NotFound();

            device.Traits.Remove(device.Traits.First(x => x.Trait == traitEnumId));

            // Save changes
            _deviceRepository.Persist();

            return RedirectToAction("Edit", "Devices", new { deviceId });
        }

        /// <summary>
        /// Edit device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="traitId">Trait id.</param>
        /// <returns>Response.</returns>
        [ImportModelState]
        public IActionResult Edit([Required] string deviceId, [Required] string traitId)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var device = _deviceRepository.Get(deviceId);

            var traitEnumId = traitId.ToEnum<TraitType>();
            if (!device.Traits.Any(x => x.Trait == traitEnumId))
                return NotFound();

            // Get trait
            var trait = device.Traits.First(x => x.Trait == traitEnumId);
            var model = new TraitViewModel
            {
                Trait = trait.Trait,
                Attributes = trait.Attributes != null ? JsonConvert.SerializeObject(trait.Attributes, Formatting.Indented) : null,
                Commands = trait.Commands != null ? JsonConvert.SerializeObject(trait.Commands, Formatting.Indented) : null,
                State = trait.State != null ? JsonConvert.SerializeObject(trait.State, Formatting.Indented) : null
            };

            return View(model);
        }

        /// <summary>
        /// Edit trait.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="traitId">Trait id.</param>
        /// <param name="viewModel">View Model.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        [ExportModelState]
        public IActionResult Edit([Required] string deviceId, [Required] string traitId, TraitViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Edit", new { deviceId, traitId });

            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var device = _deviceRepository.Get(deviceId);

            var traitEnumId = traitId.ToEnum<TraitType>();
            if (!device.Traits.Any(x => x.Trait == traitEnumId))
                return NotFound();

            // Lock the trait type just in case
            viewModel.Trait = traitEnumId;

            // Set new values
            var trait = device.Traits.FirstOrDefault(x => x.Trait == traitEnumId);

            trait.Attributes = !string.IsNullOrEmpty(viewModel.Attributes) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(viewModel.Attributes, new ObjectDictionaryConverter()) : null;
            trait.Commands = !string.IsNullOrEmpty(viewModel.Commands) ? JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(viewModel.Commands) : null;
            trait.State = !string.IsNullOrEmpty(viewModel.State) ? JsonConvert.DeserializeObject<Dictionary<string, DeviceState>>(viewModel.State) : null;

            // Save changes
            _deviceRepository.Persist();

            return RedirectToAction("Edit", "Devices", new { deviceId });
        }
    }
}
