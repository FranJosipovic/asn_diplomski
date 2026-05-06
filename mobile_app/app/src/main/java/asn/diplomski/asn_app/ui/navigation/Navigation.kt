package asn.diplomski.asn_app.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import asn.diplomski.asn_app.ui.auth.AuthRoute
import asn.diplomski.asn_app.ui.devices.DevicesRoute
import asn.diplomski.asn_app.ui.provisioning.ProvisioningRoute

sealed class Route(val route: String) {
    data object Auth : Route("auth")
    data object Devices : Route("devices/{tenantId}") {
        fun createRoute(tenantId: Long) = "devices/$tenantId"
    }
    data object Provision : Route("provision/{deviceId}") {
        fun createRoute(deviceId: Long) = "provision/$deviceId"
    }
}

@Composable
fun Navigation() {
    val navController = rememberNavController()

    NavHost(
        navController = navController,
        startDestination = Route.Auth.route
    ) {
        composable(Route.Auth.route) {
            AuthRoute(
                onNavigateToDevices = { tenantId ->
                    navController.navigate(Route.Devices.createRoute(tenantId)) {
                        popUpTo(Route.Auth.route) { inclusive = true }
                    }
                }
            )
        }

        composable(
            route = Route.Devices.route,
            arguments = listOf(navArgument("tenantId") { type = NavType.LongType })
        ) { backStackEntry ->
            val tenantId = backStackEntry.arguments?.getLong("tenantId") ?: 0L
            DevicesRoute(
                tenantId = tenantId,
                onNavigateToProvisioning = { deviceId ->
                    navController.navigate(Route.Provision.createRoute(deviceId))
                }
            )
        }

        composable(
            route = Route.Provision.route,
            arguments = listOf(navArgument("deviceId") { type = NavType.LongType })
        ) { backStackEntry ->
            val deviceId = backStackEntry.arguments?.getLong("deviceId") ?: 0L
            ProvisioningRoute(
                deviceId = deviceId,
                onNavigateBack = { navController.popBackStack() }
            )
        }
    }
}
