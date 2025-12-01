import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:google_sign_in/google_sign_in.dart';
import '../../../core/config/app_config.dart';
import '../../../core/di/injection.dart';
import '../../../data/repositories/auth_repository_impl.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';
import 'password_update_form.dart';

/// Test side til Google SSO
/// 
/// Denne side giver dig mulighed for at teste Google Sign-In integrationen.
class AuthTestPage extends StatelessWidget {
  const AuthTestPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('OAuth Login Test'),
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
      ),
      body: BlocBuilder<AuthBloc, AuthState>(
        builder: (context, state) {
          // Trigger initial check
          if (state is AuthInitial) {
            WidgetsBinding.instance.addPostFrameCallback((_) {
              context.read<AuthBloc>().add(const CheckAuthStatusEvent());
            });
          }
          return const _AuthTestContent();
        },
      ),
    );
  }
}

class _AuthTestContent extends StatelessWidget {
  const _AuthTestContent();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, state) {
        return SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              const Text(
                'OAuth Login Test',
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                'Test Google og GitHub OAuth integration',
                style: TextStyle(
                  fontSize: 16,
                  color: Colors.grey[600],
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),

              // State Display
              _buildStateCard(context, state),

              const SizedBox(height: 24),

              // Action Buttons
              if (state is! AuthLoading) ...[
                if (state is AuthUnauthenticated || state is AuthInitial) ...[
                  _buildStandardLoginForm(context),
                  const SizedBox(height: 16),
                  const Row(
                    children: [
                      Expanded(child: Divider()),
                      Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        child: Text(
                          'ELLER',
                          style: TextStyle(
                            color: Colors.grey,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      Expanded(child: Divider()),
                    ],
                  ),
                  const SizedBox(height: 16),
                  _buildGoogleLoginButton(context),
                  const SizedBox(height: 12),
                  _buildGitHubLoginButton(context),
                ],
                if (state is AuthAuthenticated) ...[
                  _buildUserInfoCard(context, state),
                  const SizedBox(height: 16),
                  _buildPasswordUpdateCard(context, state),
                  const SizedBox(height: 16),
                  _buildLogoutButton(context),
                ],
                if (state is AuthError) ...[
                  _buildErrorCard(context, state),
                  const SizedBox(height: 16),
                  _buildRetryButtons(context),
                ],
              ],

              const SizedBox(height: 32),

              // Debug Info
              _buildDebugInfo(context, state),
            ],
          ),
        );
      },
    );
  }

  Widget _buildStateCard(BuildContext context, AuthState state) {
    final Color backgroundColor;
    final IconData icon;
    final String title;
    final String? subtitle;

    if (state is AuthInitial) {
      backgroundColor = Colors.grey[200]!;
      icon = Icons.info_outline;
      title = 'Initial';
      subtitle = 'Klar til at logge ind';
    } else if (state is AuthLoading) {
      backgroundColor = Colors.blue[100]!;
      icon = Icons.hourglass_empty;
      title = 'Loading...';
      subtitle = state.loginMethod != null 
          ? 'Logger ind med ${state.loginMethod}...'
          : 'Logger ind...';
    } else if (state is AuthAuthenticated) {
      backgroundColor = Colors.green[100]!;
      icon = Icons.check_circle;
      title = 'Authenticated';
      subtitle = 'Du er logget ind!';
    } else if (state is AuthUnauthenticated) {
      backgroundColor = Colors.orange[100]!;
      icon = Icons.logout;
      title = 'Not Authenticated';
      subtitle = 'Du er ikke logget ind';
    } else if (state is AuthError) {
      backgroundColor = Colors.red[100]!;
      icon = Icons.error;
      title = 'Error';
      subtitle = state.message;
    } else {
      backgroundColor = Colors.grey[200]!;
      icon = Icons.info_outline;
      title = 'Unknown';
      subtitle = 'Ukendt tilstand';
    }

    return Card(
      color: backgroundColor,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          children: [
            Icon(icon, size: 32),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    subtitle,
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.grey[700],
                    ),
                  ),
                ],
              ),
            ),
            if (state is AuthLoading)
              const Padding(
                padding: EdgeInsets.all(8.0),
                child: CircularProgressIndicator(),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildStandardLoginForm(BuildContext context) {
    return _StandardLoginForm();
  }

  Widget _buildGoogleLoginButton(BuildContext context) {
    // Brug renderButton() for Flutter Web (anbefalet metode)
    return _GoogleSignInButtonWidget(
      onSignIn: () {
        context.read<AuthBloc>().add(const LoginWithGoogleEvent());
      },
    );
  }

  Widget _buildGitHubLoginButton(BuildContext context) {
    return ElevatedButton.icon(
      onPressed: () {
        context.read<AuthBloc>().add(const LoginWithGitHubEvent());
      },
      icon: const Icon(Icons.code),
      label: const Text('Login med GitHub'),
      style: ElevatedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 16),
        backgroundColor: const Color(0xFF24292e), // GitHub's dark color
        foregroundColor: Colors.white,
        elevation: 2,
      ),
    );
  }

  Widget _buildUserInfoCard(BuildContext context, AuthAuthenticated state) {
    final user = state.user;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Profile Picture
            if (user.picture != null) ...[
              Center(
                child: ClipOval(
                  child: Image.network(
                    // Brug backend proxy endpoint for at undgå CORS problemer
                    '${AppConfig.instance.apiBaseUrl.replaceAll('/api', '')}/api/users/${user.id}/picture',
                    width: 100,
                    height: 100,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) {
                      // Hvis proxy fejler, prøv direkte (vil sandsynligvis give CORS fejl, men bedre end ingenting)
                      return Image.network(
                        user.picture!,
                        width: 100,
                        height: 100,
                        fit: BoxFit.cover,
                        errorBuilder: (context, error, stackTrace) {
                          return const Icon(Icons.person, size: 100);
                        },
                      );
                    },
                  ),
                ),
              ),
              const SizedBox(height: 16),
            ],
            const Text(
              'Bruger Information',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            _buildInfoRow('ID', user.id.toString()),
            _buildInfoRow('Username', user.username),
            _buildInfoRow('Email', user.email),
            _buildInfoRow('Role', user.role),
            _buildInfoRow(
              'Created At',
              user.createdAt.toString().substring(0, 19),
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 8),
            const Text(
              'JWT Token Info',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            _buildJwtSection(state.authResponse.token),
            _buildInfoRow(
              'Expires',
              state.authResponse.expires.toLocal().toString().substring(0, 19),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildJwtSection(String token) {
    // Decode JWT token (base64)
    Map<String, dynamic>? decodedPayload;
    String? errorMessage;
    
    try {
      final parts = token.split('.');
      if (parts.length == 3) {
        // Decode header (part 1)
        final header = parts[0];
        final headerNormalized = base64.normalize(header);
        final headerDecoded = utf8.decode(base64.decode(headerNormalized));
        
        // Decode payload (part 2)
        final payload = parts[1];
        final payloadNormalized = base64.normalize(payload);
        final payloadDecoded = utf8.decode(base64.decode(payloadNormalized));
        
        decodedPayload = {
          'header': json.decode(headerDecoded),
          'payload': json.decode(payloadDecoded),
        };
      } else {
        errorMessage = 'Ugyldig JWT format';
      }
    } catch (e) {
      errorMessage = 'Fejl ved dekodning: $e';
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Encoded Token
        ExpansionTile(
          title: const Text(
            'JWT Token (Encoded)',
            style: TextStyle(fontWeight: FontWeight.bold),
          ),
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.grey[900],
                borderRadius: BorderRadius.circular(8),
              ),
              child: SelectableText(
                token,
                style: const TextStyle(
                  fontFamily: 'monospace',
                  fontSize: 11,
                  color: Colors.white70,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        
        // Decoded Token
        if (decodedPayload != null) ...[
          ExpansionTile(
            title: const Text(
              'JWT Header (Decoded)',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue[50],
                  borderRadius: BorderRadius.circular(8),
                ),
                child: SelectableText(
                  _formatJson(decodedPayload['header']!),
                  style: const TextStyle(
                    fontFamily: 'monospace',
                    fontSize: 12,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ExpansionTile(
            initiallyExpanded: true,
            title: const Text(
              'JWT Payload (Decoded)',
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.green[50],
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Vigtige claims i en pæn liste
                    ..._buildClaimList(decodedPayload['payload']!),
                    const Divider(height: 20),
                    // Fuld JSON
                    const Text(
                      'Fuld JSON:',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                    const SizedBox(height: 8),
                    SelectableText(
                      _formatJson(decodedPayload['payload']!),
                      style: const TextStyle(
                        fontFamily: 'monospace',
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ] else ...[
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.red[50],
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              errorMessage ?? 'Kunne ikke dekode token',
              style: const TextStyle(color: Colors.red),
            ),
          ),
        ],
      ],
    );
  }

  String _formatJson(dynamic json) {
    const encoder = JsonEncoder.withIndent('  ');
    return encoder.convert(json);
  }

  List<Widget> _buildClaimList(Map<String, dynamic> payload) {
    final importantClaims = {
      'User ID': payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      'Username': payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
      'Email': payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
      'Role': payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
      'JWT ID': payload['jti'],
      'Auth Provider': payload['auth_provider'] ?? 'OldSchool',
      'Audience': payload['aud'],
      'Expires': payload['exp'] != null 
          ? DateTime.fromMillisecondsSinceEpoch((payload['exp'] as int) * 1000)
              .toLocal()
              .toString()
              .substring(0, 19)
          : null,
    };

    return importantClaims.entries
        .where((e) => e.value != null)
        .map((e) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 4.0),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(
                    width: 100,
                    child: Text(
                      '${e.key}:',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      e.value.toString(),
                      style: const TextStyle(fontSize: 12),
                    ),
                  ),
                ],
              ),
            ))
        .toList();
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              '$label:',
              style: const TextStyle(
                fontWeight: FontWeight.w500,
                color: Colors.grey,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPasswordUpdateCard(BuildContext context, AuthAuthenticated state) {
    return PasswordUpdateForm(userId: state.user.id);
  }

  Widget _buildLogoutButton(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: () {
        context.read<AuthBloc>().add(const LogoutEvent());
      },
      icon: const Icon(Icons.logout),
      label: const Text('Logout'),
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 16),
      ),
    );
  }

  Widget _buildErrorCard(BuildContext context, AuthError state) {
    return Card(
      color: Colors.red[50],
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.error, color: Colors.red[700]),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Login Fejlede',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: Colors.red[700],
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              state.message,
              style: TextStyle(color: Colors.red[700]),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRetryButtons(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _buildGoogleLoginButton(context),
        const SizedBox(height: 12),
        _buildGitHubLoginButton(context),
      ],
    );
  }

  Widget _buildDebugInfo(BuildContext context, AuthState state) {
    return Card(
      color: Colors.grey[100],
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Debug Information',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text('State Type: ${state.runtimeType}'),
            if (state is AuthAuthenticated) ...[
              const SizedBox(height: 8),
              Text('Token Length: ${state.authResponse.token.length}'),
              Text('Has Refresh Token: ${state.authResponse.refreshToken.isNotEmpty}'),
              Text('User Email: ${state.user.email}'),
            ],
          ],
        ),
      ),
    );
  }
}

/// Google Sign-In Button Widget med renderButton() support
/// 
/// Bruger renderButton() metoden for Flutter Web, som er den anbefalede løsning.
/// Dette giver os idToken pålideligt.
class _GoogleSignInButtonWidget extends StatefulWidget {
  final VoidCallback onSignIn;

  const _GoogleSignInButtonWidget({required this.onSignIn});

  @override
  State<_GoogleSignInButtonWidget> createState() => _GoogleSignInButtonWidgetState();
}

class _GoogleSignInButtonWidgetState extends State<_GoogleSignInButtonWidget> {
  final _authRepository = getIt<AuthRepositoryImpl>();
  bool _isListening = false;

  @override
  void initState() {
    super.initState();
    _setupGoogleSignInListener();
  }

  void _setupGoogleSignInListener() {
    if (_isListening) return;
    
    // Lyt til når brugeren logger ind via Google Sign-In
    _authRepository.googleSignIn.onCurrentUserChanged.listen((GoogleSignInAccount? account) async {
      if (account != null && mounted) {
        print('✅ [DEBUG] Google user changed via renderButton: ${account.email}');
        // Trigger login flow
        widget.onSignIn();
      }
    });
    
    _isListening = true;
  }

  @override
  Widget build(BuildContext context) {
    // For Flutter Web, prøv at bruge renderButton() hvis muligt
    // Hvis ikke, fallback til normal knap
    return _buildRenderButtonOrFallback();
  }

  Widget _buildRenderButtonOrFallback() {
    // For Flutter Web, bruger vi signIn() direkte
    // renderButton() kræver dart:html og HtmlElementView, hvilket er komplekst
    // Vi bruger i stedet en knap der kalder signIn() direkte
    // Men vi lytter til onCurrentUserChanged for at fange når brugeren logger ind
    return ElevatedButton.icon(
      onPressed: () async {
        print('🔍 [DEBUG] Google Sign-In knap klikket - starter signIn()');
        try {
          // Trigger signIn() - dette vil trigger onCurrentUserChanged listener
          final account = await _authRepository.googleSignIn.signIn();
          if (account != null) {
            print('✅ [DEBUG] Google Sign-In succesfuld: ${account.email}');
            // onCurrentUserChanged listener vil kalde widget.onSignIn()
          } else {
            print('⚠️ [DEBUG] Google Sign-In blev annulleret');
          }
        } catch (e) {
          print('❌ [DEBUG] Fejl ved Google Sign-In: $e');
          // Fallback: kald onSignIn direkte
          widget.onSignIn();
        }
      },
      icon: const Icon(Icons.login),
      label: const Text('Login med Google'),
      style: ElevatedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 16),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black87,
        elevation: 2,
      ),
    );
  }
}

/// Standard Login Form Widget
/// 
/// Giver mulighed for at logge ind med username/email og password.
class _StandardLoginForm extends StatefulWidget {
  const _StandardLoginForm();

  @override
  State<_StandardLoginForm> createState() => _StandardLoginFormState();
}

class _StandardLoginFormState extends State<_StandardLoginForm> {
  final _formKey = GlobalKey<FormState>();
  final _usernameOrEmailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading = false;

  @override
  void dispose() {
    _usernameOrEmailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    if (!mounted) return;

    setState(() {
      _isLoading = true;
    });

    // Dispatch event til AuthBloc
    context.read<AuthBloc>().add(
      LoginEvent(
        usernameOrEmail: _usernameOrEmailController.text.trim(),
        password: _passwordController.text,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return BlocListener<AuthBloc, AuthState>(
      listener: (context, state) {
        if (state is AuthError && _isLoading) {
          // Vis fejl i snackbar
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: Colors.red,
            ),
          );
          setState(() {
            _isLoading = false;
          });
        } else if (state is AuthAuthenticated && _isLoading) {
          // Success - nulstil form
          _usernameOrEmailController.clear();
          _passwordController.clear();
          setState(() {
            _isLoading = false;
          });
        }
      },
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Login',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _usernameOrEmailController,
                  decoration: const InputDecoration(
                    labelText: 'Username eller Email',
                    hintText: 'Indtast dit brugernavn eller email',
                    prefixIcon: Icon(Icons.person),
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Username eller email er påkrævet';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _passwordController,
                  obscureText: _obscurePassword,
                  decoration: InputDecoration(
                    labelText: 'Password',
                    hintText: 'Indtast dit password',
                    prefixIcon: const Icon(Icons.lock),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _obscurePassword ? Icons.visibility : Icons.visibility_off,
                      ),
                      onPressed: () {
                        setState(() {
                          _obscurePassword = !_obscurePassword;
                        });
                      },
                    ),
                    border: const OutlineInputBorder(),
                  ),
                  textInputAction: TextInputAction.done,
                  onFieldSubmitted: (_) => _handleLogin(),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Password er påkrævet';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                ElevatedButton.icon(
                  onPressed: _isLoading ? null : _handleLogin,
                  icon: _isLoading
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.login),
                  label: Text(_isLoading ? 'Logger ind...' : 'Login'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

