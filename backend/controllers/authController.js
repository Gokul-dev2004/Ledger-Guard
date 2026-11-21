import User from '../models/User.js';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import nodemailer from 'nodemailer'
import crypto from 'crypto'

export const registerUser = async (req, res) => {
    try {
        const { name, email, password } = req.body;

        // Check required fields
        if (!name || !email || !password) {
            return res.status(400).json({
                success: false,
                message: 'All fields are required'
            });
        }

        // Check existing user
        const existingUser = await User.findOne({ email });

        if (existingUser) {
            return res.status(409).json({
                success: false,
                message: 'User already exists'
            });
        }

        // Hash password
        const hashedPassword = await bcrypt.hash(password, 10);

        // Create user
        const user = await User.create({
            name,
            email,
            password: hashedPassword
        });

        return res.status(201).json({
            success: true,
            message: 'User registered successfully',
            user: {
                id: user._id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });

    } catch (error) {
        return res.status(500).json({
            success: false,
            message: error.message
        });
    }
};


export const loginUser = async (req, res) => {
    try {
        const { email, password } = req.body;

        // Check required fields
        if (!email || !password) {
            return res.status(400).json({
                success: false,
                message: 'Email and password are required'
            });
        }

        // Find user
        const user = await User.findOne({ email });

        if (!user) {
            return res.status(401).json({
                success: false,
                message: 'Invalid email or password'
            });
        }

        // Compare password
        const isMatch = await bcrypt.compare(password, user.password);

        if (!isMatch) {
            return res.status(401).json({
                success: false,
                message: 'Invalid email or password'
            });
        }

        // Generate JWT token
        const token = jwt.sign(
            { id: user._id, role: user.role },
            process.env.JWT_SECRET,
            { expiresIn: '1d' }
        );

        return res.status(200).json({
            success: true,
            message: 'Login successful',
            token,
            user: {
                id: user._id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });

    } catch (error) {
        return res.status(500).json({
            success: false,
            message: error.message
        });
    }
};

export const getProfile = async (req, res) => {
    try {
        const user = await User.findById(req.user.id).select('-password');

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'User not found'
            });
        }

        return res.status(200).json({
            success: true,
            message: 'Profile fetched successfully',
            user
        });

    } catch (error) {
        return res.status(500).json({
            success: false,
            message: error.message
        });
    }
};

export const forgotPassword = async (req, res) => {
    try {
        const { email } = req.body;

        if (!email) {
            return res.status(400).json({
                success: false,
                message: 'Email is required'
            });
        }

        const user = await User.findOne({ email });

        if (!user) {
            return res.status(200).json({
                success: true,
                message: 'If the account exists, an OTP will be sent.'
            });
        }

        // Generate 6-digit OTP
        const otp = crypto.randomInt(100000, 1000000).toString();

        // Hash OTP before storing
        const hashedOTP = crypto
            .createHash('sha256')
            .update(otp)
            .digest('hex');

        user.resetOTP = hashedOTP;
        user.resetOTPExpiry = Date.now() + 5 * 60 * 1000;

        await user.save();

        // Gmail transporter
        const transporter = nodemailer.createTransport({
            service: 'gmail',
            auth: {
                user: process.env.EMAIL_USER,
                pass: process.env.EMAIL_PASS
            }
        });

        try {
            await transporter.sendMail({
                from: process.env.EMAIL_USER,
                to: user.email,
                subject: 'Ledger-Guard Password Reset OTP',
                text: `Your password reset OTP is ${otp}. It expires in 5 minutes.`
            });
        } catch (mailError) {
            user.resetOTP = undefined;
            user.resetOTPExpiry = undefined;
            await user.save();
            throw mailError;
        }

        return res.status(200).json({
            success: true,
            message: 'OTP sent successfully to your email'
        });

    } catch (error) {
        console.error('Email Error:', error);
        return res.status(500).json({
            success: false,
            message: 'Failed to send OTP'
        });
    }
};

export const verifyOTP = async (req, res) => {
    try {
        const { email, otp } = req.body;

        if (!email || !otp) {
            return res.status(400).json({
                success: false,
                message: 'Email and OTP are required'
            });
        }

        const user = await User.findOne({ email });

        if (!user || !user.resetOTP || !user.resetOTPExpiry) {
            return res.status(400).json({
                success: false,
                message: 'Invalid or expired OTP'
            });
        }

        // Check OTP expiry
        if (user.resetOTPExpiry.getTime() < Date.now()) {
            return res.status(400).json({
                success: false,
                message: 'OTP has expired'
            });
        }

        // Hash entered OTP
        const hashedOTP = crypto
            .createHash('sha256')
            .update(otp)
            .digest('hex');

        // Compare OTP
        if (hashedOTP !== user.resetOTP) {
            return res.status(400).json({
                success: false,
                message: 'Invalid OTP'
            });
        }

        const resetToken = crypto.randomBytes(32).toString('hex');

        user.resetToken = crypto
            .createHash('sha256')
            .update(resetToken)
            .digest('hex');

        user.resetTokenExpiry = Date.now() + 10 * 60 * 1000;

        user.resetOTP = undefined;
        user.resetOTPExpiry = undefined;

        await user.save();

        return res.status(200).json({
            success: true,
            message: 'OTP verified successfully',
            resetToken
        });

    } catch (error) {
        return res.status(500).json({
            success: false,
            message: 'OTP verification failed'
        });
    }
};


export const resetPassword = async (req, res) => {
  try {
    const { resetToken, newPassword } = req.body;

    if (!resetToken || !newPassword) {
      return res.status(400).json({
        success: false,
        message: 'Reset token and new password are required'
      });
    }

    if (newPassword.length < 8) {
      return res.status(400).json({
        success: false,
        message: 'Password must be at least 8 characters'
      });
    }

    const hashedToken = crypto
      .createHash('sha256')
      .update(resetToken)
      .digest('hex');

    const user = await User.findOne({
      resetToken: hashedToken,
      resetTokenExpiry: { $gt: Date.now() }
    });

    if (!user) {
      return res.status(400).json({
        success: false,
        message: 'Invalid or expired reset token'
      });
    }

    user.password = await bcrypt.hash(newPassword, 10);

    user.resetToken = undefined;
    user.resetTokenExpiry = undefined;

    await user.save();

    return res.status(200).json({
      success: true,
      message: 'Password reset successfully'
    });

  } catch (error) {
    return res.status(500).json({
      success: false,
      message: 'Password reset failed'
    });
  }
};